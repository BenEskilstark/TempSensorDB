
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

export default class FarmStore extends HTMLElement {
    farmID = null;
    farmName = "";
    token = null;
    tokenPromiseResolve = null;


    connectedCallback() {
        this.farmID = parseInt(this.getAttribute("farmID"));
        this.id = "FARM_STORE";
        fetch("/farms").then(res => res.json())
            .then(farms => {
                for (const farm of farms) {
                    if (farm.farmID == this.farmID) {
                        this.farmName = farm.name;
                        return;
                    }
                }
            })
            .catch(console.error);
    }


    loadAllSensors() {
        if (!this.farmID) return;

        return fetch(`/sensors/${this.farmID}`)
            .then(res => res.json())
            .catch(console.error);
    }


    getTokenAsync() {
        if (this.token != null) {
            return Promise.resolve(this.token);
        }

        if (localStorage.getItem("farmToken") != null) {
            this.token = localStorage.getItem("farmToken");
            return Promise.resolve(this.token);
        }

        if (this.tokenPromiseResolve) {
            return new Promise(resolve => {
                this.tokenPromiseResolve = resolve;
            });
        }

        return this.popTokenModal();
    }


    popTokenModal() {
        this.insertAdjacentHTML(
            "afterbegin",
            `<div id="modalOverlay" style="${modalStyle}">
                <div id="modalDialog" style="${dialogStyle}">
                    Must Enter Password for ${this.farmName}
                    <form id="tokenForm" action="/token" method="post">
                        <input type="password" id="password" name="password" placeholder="password"><br><br>
                        <button type="submit">Submit</button>
                    </form>
                </div>
            </div>`
        );
        const form = this.querySelector('#tokenForm');
        form.addEventListener('submit', this.handleTokenFormSubmit.bind(this));

        return new Promise(resolve => {
            this.tokenPromiseResolve = resolve;
        })
    }

    handleTokenFormSubmit(ev) {
        ev.preventDefault();
        const password = this.querySelector('#password').value;

        const farmUser = {
            farmID: this.farmID,
            name: this.farmName,
            password,
        };

        fetch('/token', {
            method: 'POST', headers: { 'Content-Type': "application/json" },
            body: JSON.stringify(farmUser),
        }).then(res => res.json())
            .then(data => {
                this.token = data.token;
                localStorage.setItem("farmToken", this.token);
                this.tokenPromiseResolve(this.token);

                const modalOverlay = this.querySelector('#modalOverlay');
                if (modalOverlay) {
                    modalOverlay.remove();
                }
                this.tokenPromiseResolve = null;
            })
            .catch(error => {
                const modalDialog = this.querySelector('#modalDialog');
                modalDialog.insertAdjacentHTML(
                    "beforeend",
                    `<div style="color: red">Incorrect Password</div>`
                );
            });
    }
}