/**
 * FruitShop - site.js
 * Xử lý: Sidebar toggle, AutoComplete AJAX, Cart badge animation
 */

// === SIDEBAR TOGGLE (Admin/Staff) ===
function toggleSidebar() {
    const sidebar  = document.getElementById('sidebar');
    const overlay  = document.getElementById('sidebarOverlay');
    const wrapper  = document.getElementById('mainWrapper');

    if (sidebar) {
        const isOpen = sidebar.classList.toggle('open');
        if (overlay) overlay.classList.toggle('show', isOpen);
    }
}

// === AUTOCOMPLETE (Tính năng sáng tạo) ===
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchInput');
    const dropdownEl  = document.getElementById('autocompleteList');

    if (!searchInput || !dropdownEl) return;

    let debounceTimer = null;

    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        const keyword = this.value.trim();

        if (keyword.length < 2) {
            hideDropdown();
            return;
        }

        // Debounce 300ms để không spam request
        debounceTimer = setTimeout(() => {
            fetch(`/Fruit/AutoComplete?keyword=${encodeURIComponent(keyword)}`)
                .then(res => res.json())
                .then(suggestions => {
                    renderAutocomplete(suggestions, keyword);
                })
                .catch(() => hideDropdown());
        }, 300);
    });

    // Đóng dropdown khi click ngoài
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.search-form')) hideDropdown();
    });

    function renderAutocomplete(suggestions, keyword) {
        if (!suggestions || suggestions.length === 0) {
            hideDropdown();
            return;
        }

        dropdownEl.innerHTML = '';
        suggestions.forEach(name => {
            const item = document.createElement('div');
            item.className = 'autocomplete-item';
            // Highlight phần match
            const escaped = keyword.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const highlighted = name.replace(new RegExp(escaped, 'gi'),
                match => `<strong class="text-success">${match}</strong>`);
            item.innerHTML = `<i class="fas fa-search text-muted" style="font-size:.75rem"></i> ${highlighted}`;
            item.addEventListener('click', () => {
                searchInput.value = name;
                hideDropdown();
                searchInput.closest('form').submit();
            });
            dropdownEl.appendChild(item);
        });

        dropdownEl.style.display = 'block';
    }

    function hideDropdown() {
        if (dropdownEl) dropdownEl.style.display = 'none';
    }
});

// === CART BADGE ANIMATION khi thêm sản phẩm ===
document.querySelectorAll('form[action*="AddToCart"]').forEach(form => {
    form.addEventListener('submit', function () {
        const badge = document.getElementById('cartBadge');
        if (badge) {
            badge.classList.remove('bounceIn');
            void badge.offsetWidth; // reflow trigger
            badge.classList.add('bounceIn');
        }
    });
});

// === AUTO-DISMISS ALERTS sau 5 giây ===
document.querySelectorAll('.alert.alert-success, .alert.alert-danger').forEach(alert => {
    setTimeout(() => {
        if (alert && alert.classList.contains('show')) {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }
    }, 5000);
});

// === LOADING SPINNER khi submit form ===
document.querySelectorAll('form:not([id="cartForm"])').forEach(form => {
    form.addEventListener('submit', function () {
        const btns = this.querySelectorAll('button[type="submit"]');
        btns.forEach(btn => {
            if (!btn.closest('td')) { // Không áp dụng cho nút nhỏ trong table
                btn.disabled = true;
                const icon = btn.querySelector('i');
                if (icon) {
                    icon.className = 'fas fa-spinner fa-spin me-2';
                }
            }
        });
    });
});
