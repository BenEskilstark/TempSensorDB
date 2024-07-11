const modalStyle = `
    display: absolute; z-index: 10;
    width:100%; height:100%;
    display: flex; justify-content: center; align-items: center;
    pointer-events: none;
`;

const dialogStyle = `
    border: 1px solid black;
    border-radius: 5px;
    padding: 20px;
    pointer-events: auto;
`;


export default class SensorPage extends HTMLElement {
    sensorID = null;
    sensor = null;
    timeRange = "Last Hour";
    pollingInterval = null;

    connectedCallback() {
        this.sensorID = this.getQueryParams().sensorID;
        this.loadSensorData();

        // Add listener to the dropdown
        const selector = document.getElementById('timeRangeSelector');
        selector.addEventListener('change', (ev) => {
            const selectedValue = ev.target.value;
            if (this.timeRange == selectedValue) return;
            this.timeRange = selectedValue;
            const [startTime, endTime] = this.getTimeRange(this.timeRange);
            this.renderChart(this.sensor.readings, startTime, endTime);
        });

        this.pollingInterval = setInterval(this.loadSensorData.bind(this), 60 * 1000);
    }

    loadSensorData() {
        return fetch(`http://temperatures.chickenkiller.com/api/v1/sensor/${encodeURIComponent(this.sensorID)}`)
            .then(r => r.json())
            .then(s => {
                this.sensor = s;
                console.log(this.sensor);
                // IMPORTANT!!!! or else it'll be in UTC time (only on Windows?)
                // s.readings.forEach(r => r.timeStamp = r.timeStamp + "Z");
                document.getElementById("title").innerHTML = `Sensor: ${s.name}`;
            })
            .then(() => {
                const lastReading = this.sensor.readings[this.sensor.readings.length - 1];
                const date = new Date(Date.parse(lastReading.timeStamp));
                const dateStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

                this.innerHTML = `
                    <div class="sensorCard">
                        Current Temperature: ${lastReading.tempF} &deg;F
                        as of ${dateStr} <br>
                        <div>
                            <button onclick="this.closest('sensor-page').popNameChangeModal()">
                                Change Sensor Name
                            </button>
                        </div>
                    </div>
                `;
            })
            .then(() => {
                const [startTime, endTime] = this.getTimeRange(this.timeRange);
                this.renderChart(this.sensor.readings, startTime, endTime);
            })
            .catch(console.error);
    }


    // ---------------------------------------------------------------------
    // Modals 
    popNameChangeModal() {
        const farmStore = document.getElementById("FARM_STORE");
        farmStore.getTokenAsync().then(token => {
            const popupModal = document.createElement('popup-modal');
            popupModal.innerHTML = `
                Enter new name for sensor ${this.sensor.name}
                <form id="nameForm" method="post">
                    <input name="name" id="name" placeholder="name"><br><br>
                    <button type="submit">Submit</button>
                </form>
            `;
            document.body.appendChild(popupModal);

            customElements.whenDefined('popup-modal').then(() => {
                // Now that the popup-modal is fully defined, we can safely query its contents
                // We can use setTimeout to wait for the end of the current execution frame
                setTimeout(() => {
                    console.log(this);
                    const form = document.getElementById('nameForm');
                    form.addEventListener('submit', (ev) => {
                        ev.preventDefault();
                        console.log("submit", this.sensor);
                        const name = form.querySelector('#name').value;
                        const id = this.sensor.sensorID;
                        this.sensor.name = name;
                        fetch(`http://temperatures.chickenkiller.com/api/v1/update-sensor/${id}`, {
                            method: "POST",
                            headers: {
                                "Content-Type": "application/json",
                                "Authorization": `Bearer ${token}`
                            },
                            body: JSON.stringify({ ...this.sensor, readings: undefined }),
                        }).then(data => {
                            const modalOverlay = this.querySelector('#POPUP_MODAL');
                            if (modalOverlay) {
                                modalOverlay.remove();
                            }
                            location.reload();
                        })
                    });
                });
            });

        })
    }

    // ---------------------------------------------------------------------
    // Render the chart 
    renderChart(readings, startTime, endTime) {
        const container = d3.select("#container");

        // Remove any existing SVG first
        container.select("svg").remove();

        // Declare the line generator.
        const line = d3.line()
            .x(r => x(new Date(r.timeStamp)))
            .y(r => y(r.tempF));


        // Declare the chart dimensions and margins.
        const clientRect = container.node().getBoundingClientRect();
        const width = clientRect.width;
        const height = Math.round(width * (2 / 4));
        const marginTop = 20;
        const marginRight = 20;
        const marginBottom = 30;
        const marginLeft = 40;

        // Declare the x (horizontal position) scale.
        const x = d3.scaleTime()
            .domain([startTime, endTime])
            .range([marginLeft, width - marginRight]);

        // Declare the y (vertical position) scale.
        const yExtent = d3.extent(readings, d => d.tempF);
        // Calculate new domain with margin
        const yDomain = [yExtent[0] - 5, yExtent[1] + 5];
        const y = d3.scaleLinear()
            .domain(yDomain)
            .range([height - marginBottom, marginTop]);

        // Create the SVG container.
        const svg = d3.create("svg")
            .attr("viewBox", `0 0 ${width} ${height}`)
            .attr("preserveAspectRatio", "xMinYMin meet")
            .style("width", "90%")
            .style("height", "auto");

        // Add the x-axis.
        svg.append("g")
            .attr("transform", `translate(0,${height - marginBottom})`)
            .call(d3.axisBottom(x)
                .ticks(width > 500 ? 12 : 6)
            );

        // Add the y-axis.
        svg.append("g")
            .attr("transform", `translate(${marginLeft},0)`)
            .call(d3.axisLeft(y));

        const filteredReadings = readings.filter(r => {
            const time = new Date(r.timeStamp);
            return time >= startTime && time <= endTime;
        });
        filteredReadings.sort((a, b) => {
            return a.readingID - b.readingID;
        });
        // console.log(filteredReadings);

        // Define a function to check the time gap between readings
        const maxGap = 5 * 60 * 1000;
        const hasGap = (current, next) => {
            const gap = Math.abs(new Date(next.timeStamp) - new Date(current.timeStamp));
            return gap > maxGap;
        }

        // Split readings into continuous groups
        const groups = [];
        let group = [];
        for (let i = 0; i < filteredReadings.length; i++) {
            group.push(filteredReadings[i]);
            if (i === filteredReadings.length - 1 ||
                hasGap(filteredReadings[i], filteredReadings[i + 1])
            ) { // End of current group
                groups.push(group);
                group = [];
            }
        }

        // Append a path for the line.
        groups.forEach(group => {
            svg.append("path")
                .datum(group) // Use the group for data binding
                .attr("fill", "none")
                .attr("stroke", "steelblue")
                .attr("stroke-width", 1.5)
                .attr("d", line);
        });

        // min and max temp lines:
        const startAndEnd = [startTime, endTime];
        if (this.sensor.minTempF != null) {
            const min = this.sensor.minTempF;
            const points = [{ timeStamp: startTime, tempF: min }, { timeStamp: endTime, tempF: min }];
            svg.append("path")
                .attr("fill", "none")
                .attr("stroke", "blue")
                .attr("stroke-width", 0.5)
                .attr("d", line(points));
        }
        if (this.sensor.maxTempF != null) {
            const max = this.sensor.maxTempF;
            const points = [{ timeStamp: startTime, tempF: max }, { timeStamp: endTime, tempF: max }];
            svg.append("path")
                .attr("fill", "none")
                .attr("stroke", "red")
                .attr("stroke-width", 0.5)
                .attr("d", line(points));
        }


        // Append the SVG element.
        container.append(() => svg.node());
    }


    // ---------------------------------------------------------------------
    // Helpers 
    getTimeRange(preset) {
        const now = new Date();
        switch (preset) {
            case 'Last Hour':
                return [d3.timeHour.offset(now, -1), now];
            case 'Last 6':
                return [d3.timeHour.offset(now, -6), now];
            case 'Last 12':
                return [d3.timeHour.offset(now, -12), now];
            case 'Last Day':
                return [d3.timeDay.offset(now, -1), now];
            case 'Last Week':
                return [d3.timeWeek.offset(now, -1), now];
            case 'Last Month':
                return [d3.timeMonth.offset(now, -1), now];
            case 'Last Year':
                return [d3.timeYear.offset(now, -1), now];
            case 'All Time':
            default:
                // Define the all-time range according to your data,
                // or dynamically determine it from your data set:
                return [
                    Math.max(
                        d3.extent(this.sensor.readings, d => new Date(d.timeStamp))[0],
                        d3.timeYear.offset(now, -1),
                    ),
                    now,
                ];
        }
    }

    getQueryParams() {
        const queryParams = {};
        const queryString = window.location.search.substring(1);
        const pairs = queryString.split("&");
        for (let i = 0; i < pairs.length; i++) {
            const pair = pairs[i].split('=');
            queryParams[decodeURIComponent(pair[0])] = decodeURIComponent(pair[1] || '');
        }
        return queryParams;
    }

}