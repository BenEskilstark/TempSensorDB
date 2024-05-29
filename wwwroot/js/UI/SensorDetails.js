export default class SensorDetails extends HTMLElement {
    sensor = null;

    connectedCallback() {
        this.sensor = JSON.parse(this.getAttribute("sensor"));
        console.log(this.sensor);
        const date = new Date(Date.parse(this.sensor.lastTimeStamp));
        this.innerHTML = `<div class="sensorCard">
            ${this.sensor.location.farm} - ${this.sensor.name}<br>
            ${this.sensor.lastTempF} &deg;F
            ${date.toLocaleTimeString()} <br>
        </div>`;
    }
}