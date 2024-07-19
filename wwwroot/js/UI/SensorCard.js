export default class SensorCard extends HTMLElement {
    sensor = null;

    connectedCallback() {
        this.sensor = JSON.parse(this.getAttribute("sensor"));
        console.log(this.sensor);

        const temp = this.sensor.lastTempF + this.sensor.calibrationValueF;
        let tempColor = "black";
        if (this.sensor.maxTempF != null && temp > this.sensor.maxTempF) {
            tempColor = "red";
        }
        if (this.sensor.minTempF != null && temp < this.sensor.minTempF) {
            tempColor = "red";
        }

        const date = new Date(this.sensor.lastTimeStamp);
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
                    Temperature Sensor Offline
                </div>
            `;
        }

        this.innerHTML = `<div class="sensorCard">
            ${this.sensor.name}<br>
            <span style="color: ${tempColor}">${temp.toFixed(2)} &deg;F</span>
            &nbsp;&nbsp;
            <span style="color: ${dateColor}">${dateStr}</span> <br>
            <a href="/sensor.html?sensorID=${this.sensor.sensorID}">Details</a>
            ${tempOfflineStr}
        </div>`;
    }
}