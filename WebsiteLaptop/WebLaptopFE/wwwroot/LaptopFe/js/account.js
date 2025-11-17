/* account.js */
(function ($) {
    'use strict';

    const CUSTOMER_API_BASE = window.CUSTOMER_API_BASE || '/api/CustomerAccount';
    const ADDRESS_API_BASE = window.ADDRESS_API_BASE || '/api/Address';

    let currentCustomerId = null;
    let provinces = [];
    const wardsCache = new Map();

    const $accountAlert = $('#accountAlert');

    /* ------------ Helpers ------------ */
    function showAlert(type, message) {
        if (!$accountAlert.length) return;
        $accountAlert
            .removeClass('d-none alert-success alert-danger alert-warning alert-info')
            .addClass(`alert-${type}`)
            .text(message);
    }

    function clearAlert() {
        if (!$accountAlert.length) return;
        $accountAlert.addClass('d-none').text('');
    }

    function formatCurrency(value) {
        return (value ?? 0).toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
    }

    function formatDate(value) {
        if (!value) return '';
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('vi-VN');
    }

    function buildStatusBadge(status) {
        const text = (status || '').toString().trim().toLowerCase();
        if (text.includes('đang xử lý') || text.includes('processing')) {
            return '<span class="badge bg-warning text-dark">Đang xử lý</span>';
        }
        if (text.includes('đã gửi') || text.includes('shipped')) {
            return '<span class="badge bg-info text-dark">Đã gửi</span>';
        }
        if (text.includes('hoàn thành') || text.includes('completed')) {
            return '<span class="badge bg-success text-white">Hoàn thành</span>';
        }
        if (text.includes('đã hủy') || text.includes('canceled') || text.includes('cancelled')) {
            return '<span class="badge bg-danger text-white">Đã hủy</span>';
        }
        return '<span class="badge bg-secondary">Khác</span>';
    }

    function loadBirthDropdowns() {
        const $day = $('#birthDay');
        const $month = $('#birthMonth');
        const $year = $('#birthYear');
        if (!$day.length || !$month.length || !$year.length) return;

        $day.empty().append('<option value="">Ngày</option>');
        $month.empty().append('<option value="">Tháng</option>');
        $year.empty().append('<option value="">Năm</option>');

        for (let d = 1; d <= 31; d++) $day.append(new Option(d, d));
        for (let m = 1; m <= 12; m++) $month.append(new Option(m, m));

        const currentYear = new Date().getFullYear();
        for (let y = currentYear; y >= 1900; y--) $year.append(new Option(y, y));
    }

    function setDateOfBirth(value) {
        if (!value) {
            $('#birthDay, #birthMonth, #birthYear').val('');
            return;
        }
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return;
        $('#birthDay').val(date.getDate());
        $('#birthMonth').val(date.getMonth() + 1);
        $('#birthYear').val(date.getFullYear());
    }

    function collectDateOfBirth() {
        const day = $('#birthDay').val();
        const month = $('#birthMonth').val();
        const year = $('#birthYear').val();
        if (!day || !month || !year) return null;
        return `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}-${day.toString().padStart(2, '0')}`;
    }

    function resolveSessionCustomerId() {
        const isLoggedIn = sessionStorage.getItem('isLoggedIn');
        const raw = sessionStorage.getItem('customer');
        if (isLoggedIn !== 'true' || !raw) return null;
        try {
            const parsed = JSON.parse(raw);
            return parsed.customerId || parsed.CustomerId || null;
        } catch (err) {
            console.error('Cannot parse customer info', err);
            return null;
        }
    }

    /* ------------ Address helpers ------------ */
    async function loadProvinces(force = false) {
        if (provinces.length && !force) return provinces;
        try {
            const response = await fetch(`${ADDRESS_API_BASE}/new-provinces`, { credentials: 'same-origin' });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            const list = Array.isArray(data?.data) ? data.data : Array.isArray(data) ? data : [];
            provinces = list.map(item => ({
                code: item.code || item.provinceCode || item.id,
                name: item.name || item.provinceName || ''
            })).filter(p => p.code && p.name);

            const $province = $('#province');
            if ($province.length) {
                $province.empty().append('<option value="">-- Chọn tỉnh/thành phố --</option>');
                provinces.forEach(p => $province.append(new Option(p.name, p.code)));
            }
            return provinces;
        } catch (err) {
            console.error('loadProvinces error', err);
            showAlert('danger', 'Không thể tải danh sách tỉnh/thành phố');
            return [];
        }
    }

    async function loadWardsByProvince(code) {
        const $ward = $('#ward');
        if (!$ward.length) return;
        $ward.prop('disabled', true).html('<option value="">-- Đang tải... --</option>');
        if (!code) {
            $ward.html('<option value="">-- Chọn xã/phường --</option>');
            return;
        }

        if (wardsCache.has(code)) {
            populateWards(wardsCache.get(code));
            return;
        }

        try {
            const response = await fetch(`${ADDRESS_API_BASE}/wards/${code}`, { credentials: 'same-origin' });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            const list = Array.isArray(data?.data) ? data.data : Array.isArray(data) ? data : [];
            const wards = list.map(w => ({
                code: w.code || w.id || w.ward_id,
                name: w.name || w.ward_name || ''
            })).filter(w => w.code && w.name);
            wardsCache.set(code, wards);
            populateWards(wards);
        } catch (err) {
            console.error('loadWards error', err);
            $ward.html('<option value="">-- Không thể tải xã/phường --</option>');
        }
    }

    function populateWards(wards) {
        const $ward = $('#ward');
        if (!$ward.length) return;
        $ward.empty().append('<option value="">-- Chọn xã/phường --</option>');
        wards.forEach(w => $ward.append(new Option(w.name, w.code)));
        $ward.prop('disabled', false);
    }

    /* ------------ Customer profile ------------ */
    async function loadCustomerData() {
        try {
            const response = await fetch(`${CUSTOMER_API_BASE}/${currentCustomerId}`, { credentials: 'same-origin' });
            if (!response.ok) throw new Error('Không thể tải thông tin khách hàng');
            const data = await response.json();
            populateProfileForm(data);
        } catch (err) {
            console.error(err);
            showAlert('danger', err.message || 'Không thể tải thông tin khách hàng');
        }
    }

    function populateProfileForm(customer) {
        $('input[name=fullname]').val(customer?.customerName || customer?.CustomerName || '');
        $('input[name=email]').val(customer?.email || customer?.Email || '');
        $('input[name=phone]').val(customer?.phoneNumber || customer?.PhoneNumber || '');
        $('#detailAddress').val(customer?.address || customer?.Address || '');
        setDateOfBirth(customer?.dateOfBirth || customer?.DateOfBirth);

        const avatar = customer?.avatar || customer?.Avatar || '../LaptopFe/img/avatar.jpg';
        $('#avatarImg').attr('src', avatar);
        $('#userName').text(customer?.customerName || customer?.CustomerName || 'Tên khách hàng');
        $('.text-muted.small').first().text(customer?.username || customer?.Username || 'username');

        disableProfileForm();
    }

    function disableProfileForm() {
        $('#profileForm input, #profileForm select, #profileForm button[type=submit]').prop('disabled', true);
        $('#toggleEditProfile').text('Chỉnh sửa');
    }

    function enableProfileForm() {
        $('#profileForm input, #profileForm select, #profileForm button[type=submit]').prop('disabled', false);
        $('#toggleEditProfile').text('Hủy');
    }

    function collectProfilePayload() {
        const fullName = $('input[name=fullname]').val()?.toString().trim();
        const email = $('input[name=email]').val()?.toString().trim();
        const phone = $('input[name=phone]').val()?.toString().trim();
        const detailAddress = $('#detailAddress').val()?.toString().trim();
        const provinceName = $('#province option:selected').text();
        const wardName = $('#ward option:selected').text();

        let address = detailAddress || '';
        if ($('#ward').val() && wardName && !wardName.startsWith('--')) {
            address = address ? `${address}, ${wardName}` : wardName;
        }
        if ($('#province').val() && provinceName && !provinceName.startsWith('--')) {
            address = address ? `${address}, ${provinceName}` : provinceName;
        }

        return {
            customerName: fullName,
            email,
            phoneNumber: phone,
            address,
            dateOfBirthString: collectDateOfBirth()
        };
    }

    async function handleProfileSubmit(e) {
        e.preventDefault();
        clearAlert();

        const payload = collectProfilePayload();
        if (!payload.customerName || !payload.email || !payload.phoneNumber) {
            showAlert('danger', 'Vui lòng nhập đầy đủ thông tin bắt buộc.');
            return;
        }

        const btn = $('#profileForm button[type=submit]');
        const original = btn.html();
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu...');

        try {
            const response = await fetch(`${CUSTOMER_API_BASE}/${currentCustomerId}/profile`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data?.message || 'Không thể cập nhật thông tin');
            showAlert('success', data.message || 'Cập nhật thông tin thành công');
            disableProfileForm();
            loadCustomerData();
        } catch (err) {
            console.error(err);
            showAlert('danger', err.message || 'Không thể cập nhật thông tin');
        } finally {
            btn.prop('disabled', false).html(original);
        }
    }

    async function handlePasswordSubmit(e) {
        e.preventDefault();
        clearAlert();

        const current = $('input[name=current]').val()?.toString().trim();
        const newPwd = $('input[name=new]').val()?.toString().trim();
        const confirm = $('input[name=confirm]').val()?.toString().trim();

        if (!current || !newPwd || !confirm) {
            showAlert('danger', 'Vui lòng nhập đầy đủ thông tin mật khẩu');
            return;
        }
        if (newPwd !== confirm) {
            showAlert('danger', 'Mật khẩu mới và xác nhận không khớp');
            return;
        }
        if (newPwd.length > 6 || !/[A-Za-z]/.test(newPwd) || !/\d/.test(newPwd)) {
            showAlert('danger', 'Mật khẩu mới phải tối đa 6 ký tự và gồm cả chữ và số');
            return;
        }

        const btn = $('#passwordForm button[type=submit]');
        const original = btn.html();
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang cập nhật...');

        try {
            const response = await fetch(`${CUSTOMER_API_BASE}/${currentCustomerId}/password`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    currentPassword: current,
                    newPassword: newPwd,
                    confirmPassword: confirm
                })
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data?.message || 'Không thể đổi mật khẩu');
            showAlert('success', data.message || 'Đổi mật khẩu thành công');
            $('#passwordForm')[0].reset();
        } catch (err) {
            console.error(err);
            showAlert('danger', err.message || 'Không thể đổi mật khẩu');
        } finally {
            btn.prop('disabled', false).html(original);
        }
    }

    /* ------------ Orders ------------ */
    function renderLoadingState(selector) {
        const $body = $(selector);
        if (!$body.length) return;
        $body.html('<tr><td colspan="5" class="text-center text-muted">Đang tải dữ liệu...</td></tr>');
    }

    function renderOrders(selector, orders, emptyMessage) {
        const $body = $(selector);
        if (!$body.length) return;
        if (!orders || !orders.length) {
            $body.html(`<tr><td colspan="5" class="text-center text-muted">${emptyMessage}</td></tr>`);
            return;
        }

        const rows = orders.map(order => {
            const orderId = order.saleInvoiceId || order.SaleInvoiceId || '';
            const date = formatDate(order.timeCreate || order.TimeCreate);
            const statusBadge = buildStatusBadge(order.status || order.Status);
            const total = formatCurrency(order.totalAmount || order.TotalAmount);
            const payload = {
                saleInvoiceId: orderId,
                date,
                status: order.status || order.Status,
                deliveryAddress: order.deliveryAddress || order.DeliveryAddress,
                paymentMethod: order.paymentMethod || order.PaymentMethod,
                deliveryFee: order.deliveryFee || order.DeliveryFee,
                totalAmount: order.totalAmount || order.TotalAmount
            };

            return `
                <tr data-order-json='${JSON.stringify(payload)}'>
                    <td>#${orderId}</td>
                    <td>${date}</td>
                    <td>${statusBadge}</td>
                    <td>${total}</td>
                    <td><button class="btn btn-sm btn-outline-primary view-order" data-order-id="${orderId}">Chi tiết</button></td>
                </tr>`;
        });

        $body.html(rows.join(''));
    }

    async function loadCustomerOrders() {
        renderLoadingState('#yourOrdersBody');
        try {
            const response = await fetch(`${CUSTOMER_API_BASE}/${currentCustomerId}/orders`, { credentials: 'same-origin' });
            if (!response.ok) throw new Error('Không thể tải đơn hàng');
            const orders = await response.json();
            renderOrders('#yourOrdersBody', orders, 'Chưa có đơn hàng nào');
        } catch (err) {
            console.error(err);
            showAlert('danger', err.message || 'Không thể tải đơn hàng');
            renderOrders('#yourOrdersBody', [], 'Không thể tải đơn hàng');
        }
    }

    async function loadCustomerHistory() {
        renderLoadingState('#orderHistoryBody');
        try {
            const response = await fetch(`${CUSTOMER_API_BASE}/${currentCustomerId}/history`, { credentials: 'same-origin' });
            if (!response.ok) throw new Error('Không thể tải lịch sử mua hàng');
            const orders = await response.json();
            renderOrders('#orderHistoryBody', orders, 'Chưa có lịch sử mua hàng');
        } catch (err) {
            console.error(err);
            showAlert('danger', err.message || 'Không thể tải lịch sử');
            renderOrders('#orderHistoryBody', [], 'Không thể tải lịch sử');
        }
    }

    /* ------------ Order detail modal ------------ */
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
                            <div id="orderDetailContent">Đang tải...</div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Đóng</button>
                        </div>
                    </div>
                </div>
            </div>`;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
    }

    function buildOrderDetailHtml(order) {
        if (!order) return '<p class="text-muted text-center">Không có dữ liệu</p>';
        const items = Array.isArray(order.items)
            ? order.items
            : Array.isArray(order.Items)
                ? order.Items
                : [];

        const rows = items.length
            ? items.map(item => `
                <tr>
                    <td>${item.ProductName || item.productName || 'N/A'}</td>
                    <td>${item.Quantity || item.quantity || 0}</td>
                    <td>${formatCurrency(item.UnitPrice || item.unitPrice)}</td>
                    <td>${formatCurrency(item.Subtotal || item.subtotal)}</td>
                </tr>`).join('')
            : '<tr><td colspan="4" class="text-center text-muted">Không có sản phẩm</td></tr>';

        return `
            <div class="mb-3">
                <p><strong>Mã đơn:</strong> ${order.SaleInvoiceId || order.saleInvoiceId}</p>
                <p><strong>Trạng thái:</strong> ${buildStatusBadge(order.Status || order.status)}</p>
                <p><strong>Thời gian:</strong> ${formatDate(order.TimeCreate || order.timeCreate)}</p>
                <p><strong>Địa chỉ giao:</strong> ${order.DeliveryAddress || order.deliveryAddress || 'Chưa cập nhật'}</p>
            </div>
            <div class="table-responsive">
                <table class="table table-sm">
                    <thead>
                        <tr>
                            <th>Sản phẩm</th>
                            <th>Số lượng</th>
                            <th>Đơn giá</th>
                            <th>Tạm tính</th>
                        </tr>
                    </thead>
                    <tbody>${rows}</tbody>
                </table>
            </div>
            <div class="d-flex justify-content-end">
                <div class="text-end">
                    <p class="mb-1"><strong>Phí giao hàng:</strong> ${formatCurrency(order.DeliveryFee || order.deliveryFee)}</p>
                    <p class="mb-0"><strong>Tổng cộng:</strong> ${formatCurrency(order.TotalAmount || order.totalAmount)}</p>
                </div>
            </div>`;
    }

    async function handleViewOrder(e) {
        e.preventDefault();
        const orderId = $(this).data('order-id');
        if (!orderId) return;
        ensureOrderModal();
        const modalEl = document.getElementById('orderDetailModal');
        const contentEl = document.getElementById('orderDetailContent');
        if (!modalEl || !contentEl) return;
        contentEl.innerHTML = '<div class="text-center text-muted py-3">Đang tải...</div>';

        try {
            const response = await fetch(`${CUSTOMER_API_BASE}/order/${orderId}`, { credentials: 'same-origin' });
            if (!response.ok) throw new Error('Không thể tải chi tiết đơn hàng');
            const order = await response.json();
            contentEl.innerHTML = buildOrderDetailHtml(order);
        } catch (err) {
            console.error(err);
            contentEl.innerHTML = `<div class="text-danger">${err.message || 'Có lỗi xảy ra'}</div>`;
        }

        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    }

    /* ------------ Event handlers ------------ */
    function handleToggleEdit() {
        const isEditing = $('#profileForm input:enabled').length > 0;
        if (isEditing) {
            disableProfileForm();
            loadCustomerData();
        } else {
            enableProfileForm();
            loadProvinces();
            $('#ward').prop('disabled', true);
        }
    }

    function handleLogout(e) {
        e.preventDefault();
        sessionStorage.removeItem('customer');
        sessionStorage.removeItem('isLoggedIn');
        redirectToLogin();
    }

    /* ------------ Init ------------ */
    $(document).ready(function () {
        loadBirthDropdowns();
        currentCustomerId = resolveSessionCustomerId();
        if (!currentCustomerId) {
            redirectToLogin();
            return;
        }

        clearAlert();
        loadCustomerData();
        loadCustomerOrders();
        loadCustomerHistory();

        $('#toggleEditProfile').on('click', handleToggleEdit);
        $('#province').on('change', function () { loadWardsByProvince(this.value); });
        $('#profileForm').on('submit', handleProfileSubmit);
        $('#passwordForm').on('submit', handlePasswordSubmit);
        $('#btnLogout').on('click', handleLogout);
        $(document).on('click', '.view-order', handleViewOrder);
    });

})(jQuery);

