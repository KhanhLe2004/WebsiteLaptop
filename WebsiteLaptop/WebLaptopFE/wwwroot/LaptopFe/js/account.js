/* js/account.js */
(function ($) {
    'use strict';
    console.log('account.js loaded');

    const API_BASE = "https://provinces.open-api.vn/api/v2";

    // Enable form
    function enableAddressForm() {
        $('#province, #ward, #detailAddress, #birthDay, #birthMonth, #birthYear').prop('disabled', false);
        loadProvinces();
        loadBirthDropdowns();
    }

    // Load provinces
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

    // Load wards (depth=3 for wards)
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
                // no wards found
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

    // Improved loadBirthDropdowns: preserves selected year if still valid
    function loadBirthDropdowns() {
        const daySel = document.getElementById("birthDay");
        const monthSel = document.getElementById("birthMonth");
        const yearSel = document.getElementById("birthYear");
        if (!daySel || !monthSel || !yearSel) return;

        const today = new Date();
        const currentYear = today.getFullYear();
        const currentMonth = today.getMonth() + 1;
        const currentDay = today.getDate();

        // Keep previously selected values to attempt to restore
        const prevSelectedYear = yearSel.value || '';
        const prevSelectedMonth = monthSel.value || '';
        const prevSelectedDay = daySel.value || '';

        // Populate days and months (always full list; keep selected if possible)
        daySel.innerHTML = '<option value="">Ngày</option>';
        for (let d = 1; d <= 31; d++) {
            const o = new Option(d, d);
            daySel.appendChild(o);
        }
        monthSel.innerHTML = '<option value="">Tháng</option>';
        for (let m = 1; m <= 12; m++) {
            monthSel.appendChild(new Option(m, m));
        }

        // Restore previous day/month if still available
        if (prevSelectedDay) daySel.value = prevSelectedDay;
        if (prevSelectedMonth) monthSel.value = prevSelectedMonth;

        // Helper: check whether a year is allowed given currently selected day/month
        function isYearAllowed(y) {
            const selectedMonth = parseInt(monthSel.value, 10);
            const selectedDay = parseInt(daySel.value, 10);

            if (isNaN(selectedMonth) || isNaN(selectedDay)) {
                // if day/month not fully selected, allow any year <= currentYear
                return y <= currentYear;
            }

            if (y < currentYear) return true;
            if (y > currentYear) return false;

            // y === currentYear -> ensure selected day/month is not after today
            if (selectedMonth > currentMonth) return false;
            if (selectedMonth < currentMonth) return true;
            // same month
            return !(selectedDay > currentDay);
        }

        // Fill years, try to restore prevSelectedYear if valid
        function fillYearsAndRestore(prevYear) {
            // clear and fill
            yearSel.innerHTML = '<option value="">Năm</option>';
            for (let y = currentYear; y >= 1900; y--) {
                if (!isYearAllowed(y)) continue;
                const o = new Option(y, y);
                yearSel.appendChild(o);
            }
            // restore
            if (prevYear) {
                const found = Array.from(yearSel.options).some(o => String(o.value) === String(prevYear));
                if (found) {
                    yearSel.value = prevYear;
                    return;
                }
            }
            yearSel.value = '';
        }

        // Event handler (avoid duplicate registration)
        function onDayOrMonthChange() {
            const prev = yearSel.value;
            fillYearsAndRestore(prev);
        }

        // Remove previously attached handlers (best-effort) then attach
        try {
            daySel.removeEventListener && daySel.removeEventListener('change', onDayOrMonthChange);
            monthSel.removeEventListener && monthSel.removeEventListener('change', onDayOrMonthChange);
        } catch (e) { /* ignore */ }
        daySel.addEventListener('change', onDayOrMonthChange);
        monthSel.addEventListener('change', onDayOrMonthChange);

        // initial fill
        fillYearsAndRestore(prevSelectedYear);
    }

    // Validate profile
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

    // Bind events when DOM ready
    $(document).ready(function () {
        // Toggle edit
        $('#toggleEditProfile').on('click', function () {
            $('input, select, button[type=submit]').prop('disabled', false);
            enableAddressForm();
        });

        // Province change -> load wards
        $('#province').on('change', function () {
            const val = this.value;
            // set loading state
            const ward = document.getElementById('ward');
            if (ward) ward.innerHTML = '<option value="">Đang tải...</option>';
            loadWardsByProvince(val);
        });

        // Profile submit
        $('#profileForm').on('submit', function (e) {
            e.preventDefault();
            if (!validateProfile()) return;

            const provinceName = $('#province option:selected').text();
            const wardName = $('#ward option:selected').text();
            const detail = $('#detailAddress').val();
            const dob = `${$('#birthDay').val()}/${$('#birthMonth').val()}/${$('#birthYear').val()}`;
            alert(`Địa chỉ đầy đủ: ${detail}, ${wardName}, ${provinceName}\nNgày sinh: ${dob}`);
        });

        // Password validate
        $('#passwordForm').on('submit', function (e) {
            e.preventDefault();
            const current = $('input[name=current]').val() ? $('input[name=current]').val().trim() : '';
            const newPwd = $('input[name=new]').val() ? $('input[name=new]').val().trim() : '';
            const confirm = $('input[name=confirm]').val() ? $('input[name=confirm]').val().trim() : '';

            const pwdRegex = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{1,6}$/;

            if (!pwdRegex.test(newPwd)) {
                alert("Mật khẩu mới phải tối đa 6 ký tự và gồm cả chữ và số!");
                return;
            }

            if (newPwd !== confirm) {
                alert("Xác nhận mật khẩu không khớp!");
                return;
            }

            if (!current) {
                alert("Vui lòng nhập mật khẩu hiện tại!");
                return;
            }

            alert("Mật khẩu đã được cập nhật thành công!");
        });
    });

})(jQuery);
