(() => {
    const getDefaultApiBase = () => {
        if (window.AUTH_API_BASE && window.AUTH_API_BASE.trim()) {
            return window.AUTH_API_BASE.trim();
        }
        if (window.location && window.location.origin) {
            const origin = window.location.origin;
            if (!origin.includes('localhost')) {
                return origin;
            }
        }
        return 'http://localhost:5068';
    };

    const AUTH_API_BASE = getDefaultApiBase();
    const ENDPOINTS = {
        login: `${AUTH_API_BASE}/api/Login`,
        register: `${AUTH_API_BASE}/api/Register`,
        forgot: `${AUTH_API_BASE}/api/FogetPasssword`
    };

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const phoneRegex = /^\d{9,15}$/;
    const passwordRegex = /^(?=.*[A-Za-z])(?=.*\d).{6,20}$/;

    const setAlert = (element, type, message) => {
        if (!element) return;
        element.textContent = message;
        element.className = `alert alert-${type}`;
        element.classList.remove('d-none');
    };

    const clearAlert = (element) => {
        if (!element) return;
        element.classList.add('d-none');
        element.textContent = '';
    };

    const parseJsonResponse = async (response) => {
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            try {
                return await response.json();
            } catch (err) {
                throw new Error('Dữ liệu JSON không hợp lệ');
            }
        }

        const text = await response.text();
        if (!text) {
            return {};
        }

        try {
            return JSON.parse(text);
        } catch {
            return { message: text };
        }
    };

    const togglePasswordVisibility = (buttonId, inputId, iconId) => {
        const btn = document.getElementById(buttonId);
        if (!btn) return;
        btn.addEventListener('click', () => {
            const input = document.getElementById(inputId);
            const icon = document.getElementById(iconId);
            if (!input || !icon) return;
            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.replace('bi-eye-slash', 'bi-eye');
            } else {
                input.type = 'password';
                icon.classList.replace('bi-eye', 'bi-eye-slash');
            }
        });
    };

    const disableButton = (button, loadingText) => {
        if (!button) return () => {};
        const original = button.innerHTML;
        button.disabled = true;
        button.innerHTML = loadingText;
        return () => {
            button.disabled = false;
            button.innerHTML = original;
        };
    };

    // ========== LOGIN ==========
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        const loginAlert = document.getElementById('loginAlert');
        const emailHelp = document.getElementById('emailHelp');
        const emailInput = document.getElementById('email');
        const passwordInput = document.getElementById('password');
        const rememberCheckbox = document.getElementById('remember');

        // Prefill remembered email
        const rememberedEmail = localStorage.getItem('rememberedEmail');
        if (rememberedEmail && emailInput) {
            emailInput.value = rememberedEmail;
            if (rememberCheckbox) rememberCheckbox.checked = true;
        }

        togglePasswordVisibility('togglePwd', 'password', 'eyeIcon');

        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            clearAlert(loginAlert);
            if (emailHelp) {
                emailHelp.style.display = 'none';
            }
            [emailInput, passwordInput].forEach(input => input?.classList.remove('is-invalid', 'input-error'));

            const credential = emailInput?.value.trim() ?? '';
            const password = passwordInput?.value ?? '';

            if (!credential) {
                emailInput?.classList.add('input-error');
                if (emailHelp) {
                    emailHelp.textContent = 'Vui lòng nhập email hoặc tên đăng nhập';
                    emailHelp.style.display = 'block';
                }
                return;
            }

            if (!password) {
                passwordInput?.classList.add('is-invalid');
                setAlert(loginAlert, 'danger', 'Vui lòng nhập mật khẩu');
                return;
            }

            const submitBtn = loginForm.querySelector('button[type="submit"]');
            const restoreBtn = disableButton(submitBtn, '<span class="spinner-border spinner-border-sm me-2"></span>Đang đăng nhập...');

            try {
                const response = await fetch(ENDPOINTS.login, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        emailOrUsername: credential,
                        password
                    })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    throw new Error(data?.message || 'Không thể đăng nhập, vui lòng thử lại');
                }

                if (data.customer) {
                    sessionStorage.setItem('customer', JSON.stringify(data.customer));
                    sessionStorage.setItem('isLoggedIn', 'true');
                    if (rememberCheckbox?.checked) {
                        localStorage.setItem('rememberedEmail', credential);
                    } else {
                        localStorage.removeItem('rememberedEmail');
                    }
                }

                setAlert(loginAlert, 'success', data.message || 'Đăng nhập thành công');
                setTimeout(() => {
                    window.location.href = '/User/Account';
                }, 600);
            } catch (error) {
                console.error('Login error:', error);
                setAlert(loginAlert, 'danger', error.message || 'Đăng nhập thất bại');
            } finally {
                restoreBtn();
            }
        });
    }

    // ========== REGISTER ==========
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        const registerAlert = document.getElementById('registerAlert');
        const fullnameInput = document.getElementById('fullname');
        const emailInput = document.getElementById('email');
        const phoneInput = document.getElementById('phone');
        const addressInput = document.getElementById('address');
        const passwordInput = document.getElementById('password');
        const confirmInput = document.getElementById('confirmPwd');
        const agreeCheckbox = document.getElementById('agree');

        togglePasswordVisibility('togglePwd', 'password', 'eyeIcon');
        togglePasswordVisibility('togglePwd2', 'confirmPwd', 'eyeIcon2');

        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            clearAlert(registerAlert);

            const inputs = [fullnameInput, emailInput, phoneInput, passwordInput, confirmInput];
            inputs.forEach(input => input?.classList.remove('is-invalid'));
            agreeCheckbox?.classList.remove('is-invalid');

            const fullName = fullnameInput?.value.trim() ?? '';
            const email = emailInput?.value.trim() ?? '';
            const phone = phoneInput?.value.trim() ?? '';
            const address = addressInput?.value.trim() ?? '';
            const password = passwordInput?.value ?? '';
            const confirmPassword = confirmInput?.value ?? '';

            let isValid = true;

            if (!fullName) {
                fullnameInput?.classList.add('is-invalid');
                isValid = false;
            }

            if (!emailRegex.test(email)) {
                emailInput?.classList.add('is-invalid');
                isValid = false;
            }

            if (!phoneRegex.test(phone)) {
                phoneInput?.classList.add('is-invalid');
                isValid = false;
            }

            if (!passwordRegex.test(password)) {
                passwordInput?.classList.add('is-invalid');
                isValid = false;
            }

            if (password !== confirmPassword || !confirmPassword) {
                confirmInput?.classList.add('is-invalid');
                isValid = false;
            }

            if (!agreeCheckbox?.checked) {
                agreeCheckbox?.classList.add('is-invalid');
                isValid = false;
            }

            if (!isValid) {
                setAlert(registerAlert, 'danger', 'Vui lòng kiểm tra lại các trường bắt buộc');
                return;
            }

            const submitBtn = registerForm.querySelector('button[type="submit"]');
            const restoreBtn = disableButton(submitBtn, '<span class="spinner-border spinner-border-sm me-2"></span>Đang đăng ký...');

            try {
                const response = await fetch(ENDPOINTS.register, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        fullName,
                        email,
                        phone,
                        address,
                        password
                    })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    throw new Error(data?.message || 'Không thể đăng ký tài khoản');
                }

                setAlert(registerAlert, 'success', data.message || 'Đăng ký thành công');
                registerForm.reset();
                setTimeout(() => {
                    window.location.href = '/User/Login';
                }, 1000);
            } catch (error) {
                console.error('Register error:', error);
                setAlert(registerAlert, 'danger', error.message || 'Đăng ký thất bại');
            } finally {
                restoreBtn();
            }
        });
    }

    // ========== FORGOT PASSWORD ==========
    const forgotForm = document.getElementById('forgotForm');
    if (forgotForm) {
        const emailInput = document.getElementById('email');
        const emailErr = document.getElementById('emailErr');
        const successBox = document.getElementById('successBox');
        const forgotError = document.getElementById('forgotError');

        forgotForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            emailErr?.classList.add('d-none');
            emailErr && (emailErr.style.display = 'none');
            emailInput?.classList.remove('is-invalid');
            clearAlert(successBox);
            clearAlert(forgotError);

            const email = emailInput?.value.trim() ?? '';
            if (!emailRegex.test(email)) {
                emailInput?.classList.add('is-invalid');
                if (emailErr) {
                    emailErr.textContent = 'Vui lòng nhập email hợp lệ';
                    emailErr.style.display = 'block';
                }
                return;
            }

            const submitBtn = forgotForm.querySelector('button[type="submit"]');
            const restoreBtn = disableButton(submitBtn, '<span class="spinner-border spinner-border-sm me-2"></span>Đang gửi...');

            try {
                const response = await fetch(ENDPOINTS.forgot, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    throw new Error(data?.message || 'Không thể gửi yêu cầu');
                }

                setAlert(successBox, 'success', data.message || 'Vui lòng kiểm tra email của bạn');
                forgotForm.reset();
            } catch (error) {
                console.error('Forgot password error:', error);
                setAlert(forgotError, 'danger', error.message || 'Không thể xử lý yêu cầu');
            } finally {
                restoreBtn();
            }
        });
    }
})();