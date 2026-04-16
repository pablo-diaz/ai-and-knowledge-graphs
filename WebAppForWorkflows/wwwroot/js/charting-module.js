import mermaid from 'https://unpkg.com/mermaid@11.14.0/dist/mermaid.esm.mjs';

let chartsCounter = 1;

async function renderMermaid(text) {
	try {
		chartsCounter++;
		const { svg } = await mermaid.render(`graphDiv${chartsCounter}`, text);
		return svg;
	} catch (err) {
		console.error("Render failed:", err);
		return `<pre style="color:red;">Chart could not be rendered.<br/><br/>${text}</pre>`;
	}
}

export async function adjustAllMermaidJsCharts(withinElement) {
	mermaid.initialize({
		startOnLoad: false,
		theme: 'forest',
		parseError: (err, hash) => {
			console.error('Mermaid syntax error:', err);
		}
	});

	const mermaidJsElements = withinElement.querySelectorAll('code.language-mermaid');
	for (const el of mermaidJsElements) {
		const newChart = document.createElement('div');
		newChart.innerHTML = await renderMermaid(el.textContent.trim());
		el.replaceWith(newChart);
	}
}
