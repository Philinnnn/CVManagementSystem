function initMarkdownRendering() {
    document.querySelectorAll('.markdown-source').forEach(src => {
        const target = src.previousElementSibling;
        if (target && target.classList.contains('markdown-render') && !target.dataset.rendered) {
            target.innerHTML = DOMPurify.sanitize(marked.parse(src.value));
            target.dataset.rendered = 'true';
        }
    });
}

document.addEventListener('DOMContentLoaded', initMarkdownRendering);