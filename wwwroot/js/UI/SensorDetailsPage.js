
export default class SensorPage extends HTMLElement {
    sensorID = null;
    sensor = null;
    timeRange = "Last Hour";

    connectedCallback() {
        this.sensorID = this.getQueryParams().sensorID;
        this.loadSensorData().then(() => {
            const lastReading = this.sensor.readings[this.sensor.readings.length - 1];
            const date = new Date(Date.parse(lastReading.timeStamp));

            this.innerHTML = `<div class="sensorCard">
                Current Temperature: ${lastReading.tempF} &deg;F
                as of ${date.toLocaleTimeString()} <br>
            </div>`;
        });

        // Add listener to the dropdown
        const selector = document.getElementById('timeRangeSelector');
        selector.addEventListener('change', (ev) => {
            const selectedValue = ev.target.value;
            if (this.timeRange == selectedValue) return;
            this.timeRange = selectedValue;
            const [startTime, endTime] = this.getTimeRange(this.timeRange);
            this.renderChart(this.sensor.readings, startTime, endTime);
        });
    }

    loadSensorData() {
        return fetch(`/sensor/${encodeURIComponent(this.sensorID)}`)
            .then(r => r.json())
            .then(s => {
                this.sensor = s;
                console.log(this.sensor);
                // IMPORTANT!!!! or else it'll be in UTC time (only on Windows?)
                // s.readings.forEach(r => r.timeStamp = r.timeStamp + "Z");
                document.getElementById("title").innerHTML = `Sensor: ${s.name}`;
            })
            .then(() => {
                const [startTime, endTime] = this.getTimeRange(this.timeRange);
                this.renderChart(this.sensor.readings, startTime, endTime);
            })
            .catch(console.error);
    }


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
        const x = d3.scaleUtc()
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

        console.log(width);
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
        console.log(filteredReadings);

        // Append a path for the line.
        svg.append("path")
            .attr("fill", "none")
            .attr("stroke", "steelblue")
            .attr("stroke-width", 1.5)
            .attr("d", line(filteredReadings));

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
                return d3.extent(this.sensor.readings, d => new Date(d.timeStamp));
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