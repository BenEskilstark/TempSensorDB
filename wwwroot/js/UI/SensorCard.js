export default class SensorCard extends HTMLElement {
    sensor = null;

    connectedCallback() {
        this.sensor = JSON.parse(this.getAttribute("sensor"));
        const date = new Date(Date.parse(this.sensor.lastTimeStamp));
        this.innerHTML = `<div class="sensorCard">
            ${this.sensor.location.farm} - ${this.sensor.name}<br>
            ${this.sensor.lastTempF} &deg;F
            ${date.toLocaleTimeString()} <br>
            <a href="/sensor.html?name=${this.sensor.name}">Details</a>
        </div>`;
    }
}