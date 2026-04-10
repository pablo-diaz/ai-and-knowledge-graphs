const sessionId = document.getElementById('txtSessionId').textContent;
const es = new EventSource(`/events?sid=${sessionId}`);
const eventContainer = document.getElementById('dvProgress');
const emptyState = document.getElementById('empty-state');

let contentCounter = 0;

es.onmessage = (e) => console.log("Generic message: ", e.data);
es.onerror = () => console.log('Reconnecting...');

es.addEventListener('finished', e => {
    es.close();
});

es.addEventListener('progressReport', e => {
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

    eventContainer.insertAdjacentHTML('beforeend', newContentHtml);

    const newTables = document.querySelectorAll(`#dvProgress #${contentId} table`);
    newTables.forEach(table => {
        table.classList.add('table', 'table-bordered', 'table-hover', 'mt-3');
    });

});