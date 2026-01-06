// Global UI helpers for Mess Management System

window.messUI = (function () {
    function showToast(message, type) {
        var container = document.querySelector('.toast-container');
        if (!container) return;
        var toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center text-bg-' + (type || 'info') + ' border-0';
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');
        toastEl.innerHTML = '<div class="d-flex">' +
            '<div class="toast-body">' + message + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';
        container.appendChild(toastEl);
        var toast = new bootstrap.Toast(toastEl, { delay: 4000 });
        toast.show();
    }

    return {
        showToast: showToast
    };
})();
