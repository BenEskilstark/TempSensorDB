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
    timeRange = "Last 6";
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
        return fetch(
            "http://temperatures.chickenkiller.com/api/v1/sensor/" +
            `${encodeURIComponent(this.sensorID)}?timeRange=${encodeURIComponent(timeRange)}`
        )
            .then(r => r.json())
            .then(s => {
                this.sensor = s;
                this.sensor.readings.sort((a, b) => {
                    return a.readingID - b.readingID;
                });
                this.sensor.readings.forEach(r => {
                    r.tempF += this.sensor.calibrationValueF;
                });
                console.log(this.sensor);
                // IMPORTANT!!!! or else it'll be in UTC time (only on Windows?)
                // s.readings.forEach(r => r.timeStamp = r.timeStamp + "Z");
                document.getElementById("title").innerHTML = `Sensor: ${s.name}`;
            })
            .then(() => {
                const lastReading = this.sensor.readings[this.sensor.readings.length - 1];
                const temp = lastReading.tempF;
                let tempColor = "black";
                if (this.sensor.maxTempF != null && temp > this.sensor.maxTempF) {
                    tempColor = "red";
                }
                if (this.sensor.minTempF != null && temp < this.sensor.minTempF) {
                    tempColor = "red";
                }

                const date = new Date(Date.parse(lastReading.timeStamp));
                let dateStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
                let dateColor = "black";
                if ((new Date()) - date > 5 * 60 * 1000) {
                    dateColor = "red";
                    dateStr = date.toLocaleString();
                }

                let tempOfflineStr = "";
                if (
                    Math.abs(new Date(this.sensor.lastHeartbeat)) -
                    Math.abs(new Date(lastReading.timeStamp)) > 2 * 60 * 1000
                ) {
                    tempOfflineStr = `
                        <div style="color: red">
                            Temperature Sensor Offline<br>
                            Last heartbeat: ${new Date(lastReading.timeStamp).toLocaleString()}
                        </div>
                    `;
                }

                this.innerHTML = `
                    <div class="sensorCard">
                        Current Temperature: 
                        <span style="color: ${tempColor}">${temp.toFixed(2)} &deg;F</span>
                        as of <span style="color: ${dateColor}">${dateStr}</span> <br>
                        ${tempOfflineStr}
                        <div>
                            <button onclick="this.closest('sensor-page').popNameChangeModal()">
                                Change Sensor Name
                            </button>
                            <button onclick="this.closest('sensor-page').popCalibrationModal()">
                                Calibrate Sensor
                            </button>
                            <button onclick="this.closest('sensor-page').popMinMaxModal()">
                                Set Min/Max Temperatures
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
                const form = document.getElementById('nameForm');
                form.addEventListener('submit', (ev) => {
                    ev.preventDefault();
                    console.log("submit", this.sensor);
                    const name = form.querySelector('#name').value;
                    this.sensor.name = name;
                    this.updateSensor(token, form);
                });
            });

        })
    }


    popCalibrationModal() {
        const farmStore = document.getElementById("FARM_STORE");
        farmStore.getTokenAsync().then(token => {
            const popupModal = document.createElement('popup-modal');
            popupModal.innerHTML = `
                Enter calibration value for sensor ${this.sensor.name}
                <form id="calibrationForm" method="post">
                    <input type="number" name="calibration" id="calibration" 
                        value=${this.sensor.calibrationValueF}
                    ><br><br>
                    <button type="submit">Submit</button>
                </form>
            `;
            document.body.appendChild(popupModal);

            customElements.whenDefined('popup-modal').then(() => {
                const form = document.getElementById('calibrationForm');
                form.addEventListener('submit', (ev) => {
                    ev.preventDefault();
                    console.log("submit", this.sensor);
                    const calibration = form.querySelector('#calibration').value;
                    this.sensor.calibrationValueF = calibration;
                    this.updateSensor(token, form);
                });
            });

        })
    }

    popMinMaxModal() {
        const farmStore = document.getElementById("FARM_STORE");
        farmStore.getTokenAsync().then(token => {
            const popupModal = document.createElement('popup-modal');
            popupModal.innerHTML = `
                Enter min/max values for sensor ${this.sensor.name}
                <form id="minForm" method="post">
                    Min: <input type="number" name="min" id="min" 
                        value=${this.sensor.minTempF}
                    ><br>
                    Max: <input type="number" name="max" id="max" 
                        value=${this.sensor.maxTempF}
                    ><br><br>
                    <button type="submit">Submit</button>
                </form>
            `;
            document.body.appendChild(popupModal);

            customElements.whenDefined('popup-modal').then(() => {
                const form = document.getElementById('minForm');
                form.addEventListener('submit', (ev) => {
                    ev.preventDefault();
                    console.log("submit", this.sensor);
                    const min = form.querySelector('#min').value;
                    const max = form.querySelector('#max').value;
                    if (min != null && min != "") {
                        this.sensor.minTempF = min;
                    } else {
                        this.sensor.minTempF = null;
                    }
                    if (max != null && max != "") {
                        this.sensor.maxTempF = max;
                    } else {
                        this.sensor.maxTempF = null;
                    }

                    this.updateSensor(token, form);
                });
            });

        })
    }

    updateSensor(token, form) {
        const id = this.sensor.sensorID;
        return fetch(`http://temperatures.chickenkiller.com/api/v1/update-sensor/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify({ ...this.sensor, readings: undefined }),
        }).then((res) => {
            if (!res.ok) {
                throw new Error("Unauthorized");
            }
            const modalOverlay = document.getElementsByTagName('popup-modal');
            if (modalOverlay.length > 0) {
                modalOverlay[0].remove();
            }
            location.reload();
        }).catch(err => {
            localStorage.removeItem("farmToken");
            form.insertAdjacentHTML(
                "afterbegin",
                `<div style="color: red">
                    Authorization invalid.<br> Try refreshing the page and 
                    entering the password again
                </div>`
            );
        });
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

        const filteredReadings = readings.filter(r => {
            if (r.timeStamp == null) return false;
            const time = new Date(r.timeStamp);
            return time >= startTime && time <= endTime;
        });
        // console.log(filteredReadings);


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
        const yExtent = d3.extent(filteredReadings, d => d.tempF);
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

        // Add the vertical line and initially set it to be invisible
        const focusLine = svg.append('line')
            .attr('class', 'focus-line')
            .style('stroke', '#666') // The color of the line
            .style('stroke-width', 1)
            .style('stroke-dasharray', '3 3') // Optional: make it a dashed line
            .style('opacity', 0)
            .attr('y1', marginTop) // Starts at the top of the chart area
            .attr('y2', height - marginBottom); // Ends at the bottom of the chart area

        const tooltip = d3.select('body').append('div')
            .attr('class', 'tooltip') // You can style it in CSS
            .style('opacity', 0);

        // Append a rect to catch mouse movements on the canvas
        svg.append('rect')
            .attr('class', 'overlay')
            .attr('width', width - marginLeft - marginRight)
            .attr('height', height - marginTop - marginBottom)
            .attr('transform', `translate(${marginLeft},${marginTop})`)
            .style('fill', 'none')
            .style('pointer-events', 'all')
            .on('mousemove touchmove', mousemove);

        function mousemove(event) {
            const mouseX = d3.pointer(event, this)[0] + marginLeft;
            const hoveredDate = x.invert(mouseX); // Convert mouseX to corresponding date
            const closestDatum = filteredReadings.reduce((prev, curr) => {
                return Math.abs(new Date(curr.timeStamp) - hoveredDate)
                    < Math.abs(new Date(prev.timeStamp) - hoveredDate) ? curr : prev;
            }); // Find data point closest to the mouse position
            focusLine.attr('transform', `translate(${mouseX},0)`)
                .style('opacity', 1); // Make the line visible

            tooltip.html(`
                Temperature: ${closestDatum.tempF}&deg;F<br>
                Time: ${new Date(closestDatum.timeStamp).toLocaleTimeString()}
                `)
                .style('opacity', 1)
                .style('left', `${event.pageX}px`) // Position tooltip at the mouse position
                .style('top', `${event.pageY - 28}px`);

        }

        // Mouse out event to hide the tooltip when the mouse leaves the overlay
        svg.on('mouseout touchend', () => {
            tooltip.style('opacity', 0);
            focusLine.style('opacity', 0);
        });


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