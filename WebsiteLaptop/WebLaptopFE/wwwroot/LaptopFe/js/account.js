/* js/account.js */
(function ($) {
    'use strict';
    console.log('account.js loaded');


    /* ===========================
       Helpers: status badge/color
       =========================== */

    // Normalize status text (lowercase, trim)
    function normStatusText(s) {
        return (s || '').toString().trim().toLowerCase();
    }

    // Return consistent badge HTML for a given status (one color per status)
    function getStatusBadge(statusText) {
        const t = normStatusText(statusText);

        // Map known statuses to single color/label
        if (t.includes('đang xử lý') || t.includes('processing')) {
            return '<span class="badge bg-warning text-dark">Đang xử lý</span>';
        }
        if (t.includes('đã gửi') || t.includes('sent') || t.includes('shipped')) {
            return '<span class="badge bg-info text-dark">Đã gửi</span>';
        }
        if (t.includes('hoàn thành') || t.includes('completed') || t.includes('completed')) {
            return '<span class="badge bg-success text-white">Hoàn thành</span>';
        }
        if (t.includes('đã hủy') || t.includes('hủy') || t.includes('cancelled') || t.includes('canceled')) {
            return '<span class="badge bg-danger text-white">Đã hủy</span>';
        }
        // default
        return '<span class="badge bg-secondary">Khác</span>';
    }

    function isCancellableStatus(statusText) {
        const t = normStatusText(statusText);
        return t.includes('đang xử lý') || t.includes('processing');
    }

    /* ===========================
       Address, birth, validation
       =========================== */

    function enableAddressForm() {
        $('#province, #ward, #detailAddress, #birthDay, #birthMonth, #birthYear').prop('disabled', false);
        loadProvinces();
        loadBirthDropdowns();
    }

    function loadProvinces() {
        const provinceSel = document.getElementById("province");
        if (!provinceSel) return;
        provinceSel.innerHTML = '<option value="">-- Chọn tỉnh/thành phố --</option>';
        fetch(`${API_BASE}/p/`)
            .then(res => res.json())
            .then(data => {
                if (!Array.isArray(data)) return;
                data.forEach(p => {
                    const opt = document.createElement("option");
                    opt.value = p.code;
                    opt.text = p.name;
                    provinceSel.appendChild(opt);
                });
            })
            .catch(err => console.error("Lỗi load tỉnh/thành:", err));
    }

    async function loadWardsByProvince(provinceCode) {
        const wardSel = document.getElementById("ward");
        if (!wardSel) return;
        wardSel.innerHTML = '<option value="">-- Chọn xã/phường --</option>';
        if (!provinceCode) {
            $('#ward').prop('disabled', true);
            return;
        }

        $('#ward').prop('disabled', true);
        try {
            const provinceRes = await fetch(`${API_BASE}/p/${provinceCode}?depth=3`);
            if (!provinceRes.ok) throw new Error('HTTP ' + provinceRes.status);
            const province = await provinceRes.json();

            let allWards = [];
            if (Array.isArray(province.districts)) {
                province.districts.forEach(district => {
                    const list = district.wards || district.communes || district.commune || [];
                    if (Array.isArray(list) && list.length) {
                        allWards = allWards.concat(list.map(w => ({ ...w, districtName: district.name })));
                    }
                });
            }

            if (allWards.length === 0) {
                wardSel.innerHTML = '<option value="">(Không có phường/xã)</option>';
                $('#ward').prop('disabled', false);
                return;
            }

            allWards.forEach(w => {
                const opt = document.createElement("option");
                opt.value = w.code ?? w.id ?? w.name;
                opt.text = `${w.name} (${w.districtName})`;
                wardSel.appendChild(opt);
            });
        } catch (err) {
            console.error("Lỗi load xã/phường:", err);
            wardSel.innerHTML = '<option value="">(Lỗi tải danh sách)</option>';
        } finally {
            $('#ward').prop('disabled', false);
        }
    }

    function loadBirthDropdowns() {
        const daySel = document.getElementById("birthDay");
        const monthSel = document.getElementById("birthMonth");
        const yearSel = document.getElementById("birthYear");
        if (!daySel || !monthSel || !yearSel) return;

        const today = new Date();
        const currentYear = today.getFullYear();
        const currentMonth = today.getMonth() + 1;
        const currentDay = today.getDate();

        const prevSelectedYear = yearSel.value || '';
        const prevSelectedMonth = monthSel.value || '';
        const prevSelectedDay = daySel.value || '';

        daySel.innerHTML = '<option value="">Ngày</option>';
        for (let d = 1; d <= 31; d++) daySel.appendChild(new Option(d, d));

        monthSel.innerHTML = '<option value="">Tháng</option>';
        for (let m = 1; m <= 12; m++) monthSel.appendChild(new Option(m, m));

        if (prevSelectedDay) daySel.value = prevSelectedDay;
        if (prevSelectedMonth) monthSel.value = prevSelectedMonth;

        function isYearAllowed(y) {
            const selectedMonth = parseInt(monthSel.value, 10);
            const selectedDay = parseInt(daySel.value, 10);
            if (isNaN(selectedMonth) || isNaN(selectedDay)) return y <= currentYear;
            if (y < currentYear) return true;
            if (y > currentYear) return false;
            if (selectedMonth > currentMonth) return false;
            if (selectedMonth < currentMonth) return true;
            return !(selectedDay > currentDay);
        }

        function fillYearsAndRestore(prevYear) {
            yearSel.innerHTML = '<option value="">Năm</option>';
            for (let y = currentYear; y >= 1900; y--) {
                if (!isYearAllowed(y)) continue;
                yearSel.appendChild(new Option(y, y));
            }
            if (prevYear) {
                const found = Array.from(yearSel.options).some(o => String(o.value) === String(prevYear));
                if (found) { yearSel.value = prevYear; return; }
            }
            yearSel.value = '';
        }

        function onDayOrMonthChange() {
            const prev = yearSel.value;
            fillYearsAndRestore(prev);
        }

        try {
            daySel.removeEventListener && daySel.removeEventListener('change', onDayOrMonthChange);
            monthSel.removeEventListener && monthSel.removeEventListener('change', onDayOrMonthChange);
        } catch (e) { /* ignore */ }

        daySel.addEventListener('change', onDayOrMonthChange);
        monthSel.addEventListener('change', onDayOrMonthChange);

        fillYearsAndRestore(prevSelectedYear);
    }

    function validateProfile() {
        const fullname = $('input[name=fullname]').val() ? $('input[name=fullname]').val().trim() : '';
        const email = $('input[name=email]').val() ? $('input[name=email]').val().trim() : '';
        const phone = $('input[name=phone]').val() ? $('input[name=phone]').val().trim() : '';

        const nameRegex = /^[a-zA-ZÀ-ỹ\s]+$/u;
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const phoneRegex = /^[0-9]{9,15}$/;

        if (!nameRegex.test(fullname)) { alert("Tên không hợp lệ"); return false; }
        if (!emailRegex.test(email)) { alert("Email không hợp lệ"); return false; }
        if (!phoneRegex.test(phone)) { alert("Số điện thoại không hợp lệ"); return false; }

        return true;
    }

    /* ===========================
       Order modal, render, fetch, build
       =========================== */

    function ensureOrderModal() {
        if (document.getElementById('orderDetailModal')) return;
        const modalHtml = `
      <div class="modal fade" id="orderDetailModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Chi tiết đơn hàng</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
              <div id="orderDetailContent"></div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Đóng</button>
            </div>
          </div>
        </div>
      </div>
    `;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
    }

    function escapeHtml(str) {
        if (str === null || str === undefined) return '';
        return String(str)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function formatCurrency(n) {
        n = Number(n || 0);
        return n.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
    }

    function parseNumberFromCurrency(text) {
        if (!text) return 0;
        const cleaned = text.replace(/[^\d,.-]/g, '').replace(/\./g, '').replace(/,/g, '');
        const n = parseFloat(cleaned);
        return isNaN(n) ? 0 : n;
    }

    function renderOrderDetailHtml(order) {
        const items = (order.items || []).map(it => `
      <tr>
        <td>${escapeHtml(it.name)}</td>
        <td class="text-center">${escapeHtml(String(it.qty))}</td>
        <td class="text-end">${formatCurrency(it.price)}</td>
        <td class="text-end">${formatCurrency((it.price || 0) * (it.qty || 1))}</td>
      </tr>
    `).join('');

        const itemsTable = `
      <div class="table-responsive">
        <table class="table table-sm">
          <thead class="table-light">
            <tr><th>Sản phẩm</th><th class="text-center">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th></tr>
          </thead>
          <tbody>${items}</tbody>
        </table>
      </div>
    `;

        return `
      <div>
        <p><strong>Mã đơn:</strong> ${escapeHtml(order.code || order.id || '')}</p>
        <p><strong>Ngày đặt:</strong> ${escapeHtml(order.date || '')}</p>
        <p><strong>Trạng thái:</strong> ${escapeHtml(order.status || '')}</p>
        <p><strong>Địa chỉ giao:</strong> ${escapeHtml(order.address || '')}</p>
        <hr>
        ${itemsTable}
        <div class="d-flex justify-content-end">
          <div>
            <p class="mb-1">Tạm tính: <strong>${formatCurrency(order.subtotal || order.total || 0)}</strong></p>
            <p class="mb-1">Phí vận chuyển: <strong>${formatCurrency(order.shipping || 0)}</strong></p>
            <p class="mb-1">Tổng cộng: <strong>${formatCurrency(order.total || order.subtotal || 0)}</strong></p>
          </div>
        </div>
      </div>
    `;
    }

    async function fetchOrderById(orderId) {
        if (!orderId) return null;
        try {
            const res = await fetch(`/api/orders/${encodeURIComponent(orderId)}`, { method: 'GET', credentials: 'same-origin' });
            if (!res.ok) return null;
            return await res.json();
        } catch (e) {
            console.warn('fetchOrderById failed', e);
            return null;
        }
    }

    function buildOrderFromRow($tr) {
        const code = $tr.find('td').eq(0).text().trim();
        const date = $tr.find('td').eq(1).text().trim();
        const status = $tr.find('td').eq(2).text().trim();
        const totalText = $tr.find('td').eq(3).text().trim();
        const total = parseNumberFromCurrency(totalText);
        const dataJson = $tr.attr('data-order-json');
        let items = [];
        if (dataJson) {
            try { const parsed = JSON.parse(dataJson); if (Array.isArray(parsed.items)) items = parsed.items; if (parsed.address) parsed.address; } catch (e) { }
        }
        if (items.length === 0) {
            items = [{ name: 'Sản phẩm mẫu', qty: 1, price: total || 0 }];
        }
        const address = $tr.attr('data-order-address') || '';
        return { code, date, status, total, subtotal: total, shipping: 0, items, address };
    }

    // Show modal and set footer buttons (cancel next to close)
    async function showOrderModalForRow($tr) {
        ensureOrderModal();
        const modalEl = document.getElementById('orderDetailModal');
        const contentEl = document.getElementById('orderDetailContent');
        if (!modalEl || !contentEl) return;

        const orderId = $tr.attr('data-order-id') || $tr.find('td').eq(0).text().trim();
        let order = null;
        if ($tr.attr('data-order-id')) {
            order = await fetchOrderById($tr.attr('data-order-id'));
        }
        if (!order) {
            order = buildOrderFromRow($tr);
        }

        // Render modal body
        contentEl.innerHTML = renderOrderDetailHtml(order);

        // Update modal footer: ensure there's a single Cancel button only if cancellable
        const footer = modalEl.querySelector('.modal-footer');
        if (footer) {
            // remove any existing cancel button we previously inserted
            const existingCancel = footer.querySelector('#cancelOrderBtn');
            if (existingCancel) existingCancel.remove();

            // find close button in footer (we keep it) and insert cancel BEFORE it so they appear side-by-side
            const closeBtn = footer.querySelector('[data-bs-dismiss="modal"]');
            if (isCancellableStatus(order.status)) {
                const cancelBtn = document.createElement('button');
                cancelBtn.id = 'cancelOrderBtn';
                cancelBtn.type = 'button';
                cancelBtn.className = 'btn btn-danger';
                cancelBtn.innerHTML = '<i class="fas fa-times-circle"></i> Hủy đơn hàng';
                // insert before close button so both are horizontal
                if (closeBtn) footer.insertBefore(cancelBtn, closeBtn);
                else footer.appendChild(cancelBtn);
            }
        }

        // store current row reference on modal for cancel handler
        modalEl.__currentRow = $tr.get(0);

        // show modal
        const modal = new bootstrap.Modal(modalEl);
        modal.show();

        // After showing, also update the status text in body (consistent coloring) if present
        const statusP = contentEl.querySelector('p strong');
        // nothing needed; badge in table will be updated separately when needed
    }

    /* ===========================
       Segregate orders (completed -> history)
       =========================== */

    function isCompletedStatusText(text) {
        if (!text) return false;
        const t = normStatusText(text);
        return t.includes('hoàn thành');
    }

    function segregateOrdersOnLoad() {
        try {
            const $yourBody = $('#yourOrdersBody');
            const $historyBody = $('#orderHistoryBody');
            if (!$yourBody.length || !$historyBody.length) return;

            // collect from both bodies
            const allRows = $yourBody.find('tr').toArray().concat($historyBody.find('tr').toArray());

            allRows.forEach(row => {
                const $r = $(row);
                // determine status
                let statusText = '';
                const dataJson = $r.attr('data-order-json');
                if (dataJson) {
                    try { const parsed = JSON.parse(dataJson); statusText = parsed.status || ''; } catch (e) { statusText = ''; }
                }
                if (!statusText) statusText = $r.find('td').eq(2).text().trim();

                // normalize and set badge cell to consistent color
                $r.find('td').eq(2).html(getStatusBadge(statusText));

                if (isCompletedStatusText(statusText)) {
                    if ($r.closest('#orderHistoryBody').length === 0) $historyBody.append($r);
                } else {
                    if ($r.closest('#yourOrdersBody').length === 0) $yourBody.append($r);
                }
            });
        } catch (e) {
            console.error('segregateOrdersOnLoad error', e);
        }
    }

    /* ===========================
       Binding events
       =========================== */

    $(document).ready(function () {
        // on load segregate
        segregateOrdersOnLoad();

        // toggle edit
        $('#toggleEditProfile').on('click', function () {
            $('input, select, button[type=submit]').prop('disabled', false);
            enableAddressForm();
        });

        // province -> wards
        $('#province').on('change', function () {
            const val = this.value;
            const ward = document.getElementById('ward');
            if (ward) ward.innerHTML = '<option value="">Đang tải...</option>';
            loadWardsByProvince(val);
        });

        // profile submit
        $('#profileForm').on('submit', function (e) {
            e.preventDefault();
            if (!validateProfile()) return;
            const provinceName = $('#province option:selected').text();
            const wardName = $('#ward option:selected').text();
            const detail = $('#detailAddress').val();
            const dob = `${$('#birthDay').val()}/${$('#birthMonth').val()}/${$('#birthYear').val()}`;
            alert(`Địa chỉ đầy đủ: ${detail}, ${wardName}, ${provinceName}\nNgày sinh: ${dob}`);
        });

        // password submit
        $('#passwordForm').on('submit', function (e) {
            e.preventDefault();
            const current = $('input[name=current]').val() ? $('input[name=current]').val().trim() : '';
            const newPwd = $('input[name=new]').val() ? $('input[name=new]').val().trim() : '';
            const confirm = $('input[name=confirm]').val() ? $('input[name=confirm]').val().trim() : '';

            const pwdRegex = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{1,6}$/;
            if (!pwdRegex.test(newPwd)) { alert("Mật khẩu mới phải tối đa 6 ký tự và gồm cả chữ và số!"); return; }
            if (newPwd !== confirm) { alert("Xác nhận mật khẩu không khớp!"); return; }
            if (!current) { alert("Vui lòng nhập mật khẩu hiện tại!"); return; }
            alert("Mật khẩu đã được cập nhật thành công!");
        });

        // view order
        $(document).on('click', '.view-order', function (e) {
            e.preventDefault();
            const $btn = $(this);
            const $tr = $btn.closest('tr');
            if (!$tr || !$tr.length) return;
            showOrderModalForRow($tr);
        });

        // cancel order button in footer (UI-only)
        $(document).on('click', '#cancelOrderBtn', function (e) {
            e.preventDefault();
            if (!confirm('Bạn có chắc muốn hủy đơn hàng này?')) return;

            const modalEl = document.getElementById('orderDetailModal');
            const rowEl = modalEl && modalEl.__currentRow ? modalEl.__currentRow : null;
            if (rowEl) {
                const $row = $(rowEl);
                // set status in table to "Đã hủy" and apply consistent badge color
                $row.find('td').eq(2).html(getStatusBadge('Đã hủy'));

                // if it was in history (shouldn't be for cancellable), move back to your orders
                if ($row.closest('#orderHistoryBody').length) {
                    $('#yourOrdersBody').append($row);
                }
            }

            // optionally: send cancel to server (not implemented) — you can add fetch to /api/orders/{id}/cancel here

            alert('Đơn hàng đã được hủy (UI).');

            // close modal
            const modalInstance = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
            if (modalInstance) modalInstance.hide();
        });
    });

})(jQuery);
