let contentCounter = 0;
let isReceivingMessages = false;

const infoContainer = document.getElementById('txtInformation');

const cancelButton = document.getElementById('btnCancel');
cancelButton.addEventListener('click', e => {
    if (isReceivingMessages) {
        es.close();
        isReceivingMessages = false;
        cancelButton.style.display = 'none';
        infoContainer.textContent = "Cancelled";
    }
    e.preventDefault();
});

const sessionId = document.getElementById('txtSessionId').textContent;
const es = new EventSource(`/events?sid=${sessionId}`);

es.onopen = () => {
    cancelButton.style.display = 'block';
    isReceivingMessages = true;
};

es.onmessage = (e) => console.warn(`Unhandled message of type '${e.event}' with message content: '${e.data}'`);

es.onerror = () => console.log('Reconnecting...');

es.addEventListener('finished', e => {
    es.close();
    infoContainer.textContent = "Finished";
    cancelButton.style.display = 'none';
});

es.addEventListener('errorsReport', e => console.error(e.data));

es.addEventListener('warningsReport', e => console.warn(e.data));

es.addEventListener('queryCreated', e => console.info(`Query to execute: ${e.data}`));

es.addEventListener('progressReport', e => {
    const message = JSON.parse(e.data);
    console.info(message.content);
    infoContainer.textContent = message.content;
});

es.addEventListener('userResponse', e => {
    const emptyState = document.getElementById('txt-to-remove');
    if (emptyState) emptyState.remove();

    const message = JSON.parse(e.data);

    marked.setOptions({
        gfm: true,
        breaks: true,
        smartLists: true
    });

    const transformedHtmlMessage = marked.parse(message.content);

    contentCounter++;
    const contentId = `txtContent${contentCounter}`;
    const newContentHtml = `
        <div id="${contentId}" class="border p-3 bg-light rounded">
            ${transformedHtmlMessage}
        </div>
    `;

    const eventContainer = document.getElementById('dvFinalAnswer');
    eventContainer.insertAdjacentHTML('beforeend', newContentHtml);

    const newTables = document.querySelectorAll(`#dvFinalAnswer #${contentId} table`);
    newTables.forEach(table => {
        table.classList.add('table', 'table-bordered', 'table-hover', 'mt-3');
    });

    import('./charting-module.js')
    .then((chartingModule) => {
        chartingModule.adjustAllMermaidJsCharts(document.querySelector(`#dvFinalAnswer #${contentId}`));
    })
    .catch((err) => console.error('Error loading charting module:', err));

});
