
const modalStyle = `
    position: absolute; z-index: 10;
    top: 0; left: 0;
    width:100%; height:100%;
    display: flex; justify-content: center; align-items: center;
    pointer-events: none;
    background-color: rgba(0,0,0, 0.5);
`;

const dialogStyle = `
    border: 1px solid black;
    border-radius: 5px;
    padding: 20px;
    pointer-events: auto;
    background-color: white;
    position: relative;
`;

const closeButtonStyle = `
    position: absolute;
    top: 0;
    right: 0;
    border: none;
    background: none;
    cursor: pointer;
    font-size: 1em;
    padding: 0.2em 0.5em; /* Padding for visual appearance */
`;

export default class PopupModal extends HTMLElement {
    constructor() {
        super(); // Always call super first in constructor
        this.popup = this.createModal();
        this.attachCloseButtonListener();
    }

    createModal() {
        // Create the modal container
        const modal = document.createElement('div');
        modal.setAttribute('style', modalStyle);

        // Create the dialog container
        const dialog = document.createElement('div');
        dialog.setAttribute('style', dialogStyle);

        // Create the close button
        const closeButton = document.createElement('button');
        closeButton.textContent = '❌';
        closeButton.setAttribute('style', closeButtonStyle);
        closeButton.id = 'closeButton'; // An ID for easy access
        dialog.appendChild(closeButton);

        // Append dialog to modal
        modal.appendChild(dialog);

        return { modal, dialog };
    }

    attachCloseButtonListener() {
        // Attach an event listener to the close button
        const closeButton = this.popup.dialog.querySelector('#closeButton');
        closeButton.addEventListener('click', () => {
            this.remove(); // Remove the custom element itself
        });
    }

    connectedCallback() {
        // Temporarily detach the modal from the custom element before moving the other children.
        // This prevents the 'appendChild' call from trying to append the modal or a descendant of it.
        this.popup.modal.remove();

        // Move all other children to the dialog container.
        while (this.childNodes.length > 0) {
            this.popup.dialog.appendChild(this.firstChild);
        }

        // Re-append the modal container after all other children have been moved.
        this.appendChild(this.popup.modal);
    }
}