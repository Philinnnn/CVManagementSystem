function initImageUploadWidgets() {
    document.querySelectorAll('.image-upload-widget').forEach(widget => {
        const btn = widget.querySelector('.upload-image-btn');
        const input = widget.querySelector('.image-url-input');
        const img = widget.querySelector('img');

        btn.addEventListener('click', () => {
            const uploadWidget = cloudinary.createUploadWidget(
                {
                    cloudName: window.CLOUDINARY_CLOUD_NAME,
                    uploadPreset: window.CLOUDINARY_UPLOAD_PRESET,
                    sources: ['local', 'url', 'camera'],
                    multiple: false,
                    maxFiles: 1
                },
                (error, result) => {
                    if (!error && result && result.event === 'success') {
                        const url = result.info.secure_url;
                        input.value = url;
                        img.src = url;
                        img.classList.remove('d-none');
                    }
                }
            );
            uploadWidget.open();
        });
    });
}

document.addEventListener('DOMContentLoaded', initImageUploadWidgets);