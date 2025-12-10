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
        forgot: `${AUTH_API_BASE}/api/FogetPasssword`,
        googleLogin: `${AUTH_API_BASE}/api/Login/google`,
        facebookLogin: `${AUTH_API_BASE}/api/Login/facebook`
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
                throw new Error('D? li?u JSON kh�ng h?p l?');
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
                    emailHelp.textContent = 'Vui l�ng nh?p email ho?c t�n dang nh?p';
                    emailHelp.style.display = 'block';
                }
                return;
            }

            if (!password) {
                passwordInput?.classList.add('is-invalid');
                setAlert(loginAlert, 'danger', 'Vui l�ng nh?p m?t kh?u');
                return;
            }

            const submitBtn = loginForm.querySelector('button[type="submit"]');
            const restoreBtn = disableButton(submitBtn, '<span class="spinner-border spinner-border-sm me-2"></span>�ang dang nh?p...');

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
                    throw new Error(data?.message || 'Kh�ng th? dang nh?p, vui l�ng th? l?i');
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

                setAlert(loginAlert, 'success', data.message || '�ang nh?p th�nh c�ng');
                setTimeout(() => {
                    window.location.href = '/User/Account';
                }, 600);
            } catch (error) {
                console.error('Login error:', error);
                setAlert(loginAlert, 'danger', error.message || '�ang nh?p th?t b?i');
            } finally {
                restoreBtn();
            }
        });
    }

    // ========== GOOGLE LOGIN ==========
    const googleSignInBtn = document.getElementById('googleSignInBtn');
    if (googleSignInBtn) {
        // Lấy Google Client ID
        const getGoogleClientId = () => {
            return window.GOOGLE_CLIENT_ID || '';
        };

        // Xử lý đăng nhập với access token và user info
        const handleGoogleLogin = async (accessToken, userInfo) => {
            const loginAlert = document.getElementById('loginAlert');
            if (loginAlert) {
                clearAlert(loginAlert);
            }

            const submitBtn = googleSignInBtn;
            const originalText = submitBtn.innerHTML;
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';

            try {
                const response = await fetch(ENDPOINTS.googleLogin, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        accessToken: accessToken,
                        userInfo: {
                            email: userInfo.email,
                            name: userInfo.name,
                            picture: userInfo.picture
                        }
                    })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    throw new Error(data?.message || 'Không thể đăng nhập bằng Google');
                }

                if (data.customer) {
                    sessionStorage.setItem('customer', JSON.stringify(data.customer));
                    sessionStorage.setItem('isLoggedIn', 'true');
                }

                if (loginAlert) {
                    setAlert(loginAlert, 'success', data.message || 'Đăng nhập bằng Google thành công');
                }
                setTimeout(() => {
                    window.location.href = '/User/Account';
                }, 600);
            } catch (error) {
                console.error('Google login error:', error);
                if (loginAlert) {
                    setAlert(loginAlert, 'danger', error.message || 'Đăng nhập bằng Google thất bại');
                }
            } finally {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        };

        // Xử lý khi user click nút Google
        googleSignInBtn.addEventListener('click', async () => {
            const clientId = getGoogleClientId();
            
            if (!clientId) {
                const loginAlert = document.getElementById('loginAlert');
                if (loginAlert) {
                    setAlert(loginAlert, 'warning', 'Google OAuth Client ID chưa được cấu hình. Vui lòng liên hệ quản trị viên.');
                }
                return;
            }

            // Đợi Google script load xong
            const waitForGoogle = () => {
                return new Promise((resolve) => {
                    if (typeof google !== 'undefined' && google.accounts && google.accounts.oauth2) {
                        resolve();
                    } else {
                        let attempts = 0;
                        const checkInterval = setInterval(() => {
                            attempts++;
                            if (typeof google !== 'undefined' && google.accounts && google.accounts.oauth2) {
                                clearInterval(checkInterval);
                                resolve();
                            } else if (attempts > 20) { // Đợi tối đa 10 giây
                                clearInterval(checkInterval);
                                resolve(); // Vẫn resolve để hiển thị lỗi
                            }
                        }, 500);
                    }
                });
            };

            await waitForGoogle();

            if (typeof google === 'undefined' || !google.accounts || !google.accounts.oauth2) {
                const loginAlert = document.getElementById('loginAlert');
                if (loginAlert) {
                    setAlert(loginAlert, 'danger', 'Không thể tải Google Sign-In. Vui lòng kiểm tra kết nối internet và thử lại.');
                }
                return;
            }

            try {
                // Sử dụng OAuth2 flow để lấy access token
                let tokenClient = null;
                
                tokenClient = google.accounts.oauth2.initTokenClient({
                    client_id: clientId,
                    scope: 'openid email profile',
                    callback: async (tokenResponse) => {
                        if (tokenResponse.error) {
                            console.error('Google OAuth error:', tokenResponse.error);
                            const loginAlert = document.getElementById('loginAlert');
                            if (loginAlert) {
                                setAlert(loginAlert, 'danger', 'Lỗi đăng nhập Google: ' + (tokenResponse.error_description || tokenResponse.error));
                            }
                            return;
                        }

                        if (tokenResponse.access_token) {
                            try {
                                // Lấy user info từ Google
                                const userInfoResponse = await fetch('https://www.googleapis.com/oauth2/v2/userinfo?access_token=' + tokenResponse.access_token);
                                if (!userInfoResponse.ok) {
                                    const errorText = await userInfoResponse.text();
                                    throw new Error('Không thể lấy thông tin từ Google: ' + errorText);
                                }
                                const userInfo = await userInfoResponse.json();
                                
                                // Gửi về backend để xử lý
                                await handleGoogleLogin(tokenResponse.access_token, userInfo);
                            } catch (error) {
                                console.error('Error fetching user info:', error);
                                const loginAlert = document.getElementById('loginAlert');
                                if (loginAlert) {
                                    setAlert(loginAlert, 'danger', 'Không thể lấy thông tin từ Google: ' + error.message);
                                }
                            }
                        }
                    }
                });

                // Yêu cầu access token
                tokenClient.requestAccessToken();
            } catch (error) {
                console.error('Error initializing Google OAuth:', error);
                const loginAlert = document.getElementById('loginAlert');
                if (loginAlert) {
                    setAlert(loginAlert, 'danger', 'Lỗi khởi tạo Google Sign-In: ' + error.message);
                }
            }
        });
    }

    // ========== FACEBOOK LOGIN ==========
    const facebookSignInBtn = document.getElementById('facebookSignInBtn');
    if (facebookSignInBtn) {
        // Lấy Facebook App ID
        const getFacebookAppId = () => {
            return window.FACEBOOK_APP_ID || '';
        };

        // Xử lý đăng nhập với access token
        const handleFacebookLogin = async (accessToken) => {
            const loginAlert = document.getElementById('loginAlert');
            if (loginAlert) {
                clearAlert(loginAlert);
            }

            const submitBtn = facebookSignInBtn;
            const originalText = submitBtn.innerHTML;
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';

            try {
                const response = await fetch(ENDPOINTS.facebookLogin, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ accessToken })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    // Tạo error object với thông tin chi tiết
                    const error = new Error(data?.message || 'Không thể đăng nhập bằng Facebook');
                    error.details = data?.details || data?.error || '';
                    throw error;
                }

                if (data.customer) {
                    sessionStorage.setItem('customer', JSON.stringify(data.customer));
                    sessionStorage.setItem('isLoggedIn', 'true');
                }

                if (loginAlert) {
                    setAlert(loginAlert, 'success', data.message || 'Đăng nhập bằng Facebook thành công');
                }
                setTimeout(() => {
                    window.location.href = '/User/Account';
                }, 600);
            } catch (error) {
                console.error('Facebook login error:', error);
                if (loginAlert) {
                    // Hiển thị lỗi chi tiết hơn
                    let errorMessage = error.message || 'Đăng nhập bằng Facebook thất bại';
                    if (error.details) {
                        errorMessage += ': ' + error.details;
                    }
                    setAlert(loginAlert, 'danger', errorMessage);
                }
            } finally {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        };

        // Xử lý token từ query string (fallback nếu popup không hoạt động)
        const urlParams = new URLSearchParams(window.location.search);
        const fbToken = urlParams.get('fb_token');
        const fbError = urlParams.get('fb_error');
        
        if (fbToken) {
            // Xóa token khỏi URL
            window.history.replaceState(null, null, window.location.pathname);
            handleFacebookLogin(fbToken);
        } else if (fbError) {
            // Xóa error khỏi URL
            window.history.replaceState(null, null, window.location.pathname);
            const loginAlert = document.getElementById('loginAlert');
            if (loginAlert) {
                setAlert(loginAlert, 'danger', decodeURIComponent(fbError));
            }
        }

        // Đợi Facebook SDK load
        const waitForFacebook = () => {
            return new Promise((resolve) => {
                if (typeof FB !== 'undefined') {
                    resolve();
                } else {
                    let attempts = 0;
                    const checkInterval = setInterval(() => {
                        attempts++;
                        if (typeof FB !== 'undefined') {
                            clearInterval(checkInterval);
                            resolve();
                        } else if (attempts > 20) { // Đợi tối đa 10 giây
                            clearInterval(checkInterval);
                            resolve(); // Vẫn resolve để hiển thị lỗi
                        }
                    }, 500);
                }
            });
        };

        // Xử lý khi user click nút Facebook
        facebookSignInBtn.addEventListener('click', async () => {
            const appId = getFacebookAppId();
            
            if (!appId) {
                const loginAlert = document.getElementById('loginAlert');
                if (loginAlert) {
                    setAlert(loginAlert, 'warning', 'Facebook App ID chưa được cấu hình. Vui lòng liên hệ quản trị viên.');
                }
                return;
            }

            // Sử dụng popup window để đăng nhập Facebook (hoạt động với cả HTTP và HTTPS)
            const redirectUri = encodeURIComponent(window.location.origin + '/User/FacebookCallback');
            const facebookAuthUrl = `https://www.facebook.com/v18.0/dialog/oauth?client_id=${appId}&redirect_uri=${redirectUri}&scope=public_profile,email&response_type=token&display=popup`;
            
            // Mở popup
            const width = 600;
            const height = 700;
            const left = (window.screen.width - width) / 2;
            const top = (window.screen.height - height) / 2;
            
            const popup = window.open(
                facebookAuthUrl,
                'Facebook Login',
                `width=${width},height=${height},left=${left},top=${top},toolbar=no,menubar=no,scrollbars=yes,resizable=yes,location=no,directories=no,status=no`
            );
            
            if (!popup) {
                const loginAlert = document.getElementById('loginAlert');
                if (loginAlert) {
                    setAlert(loginAlert, 'warning', 'Popup bị chặn. Vui lòng cho phép popup và thử lại.');
                }
                return;
            }
            
            // Lắng nghe message từ popup
            const messageListener = (event) => {
                // Kiểm tra origin để đảm bảo an toàn
                if (event.origin !== window.location.origin) {
                    return;
                }
                
                if (event.data && event.data.type === 'FACEBOOK_LOGIN_SUCCESS') {
                    clearInterval(checkPopup);
                    window.removeEventListener('message', messageListener);
                    if (popup && !popup.closed) {
                        popup.close();
                    }
                    handleFacebookLogin(event.data.accessToken);
                } else if (event.data && event.data.type === 'FACEBOOK_LOGIN_ERROR') {
                    clearInterval(checkPopup);
                    window.removeEventListener('message', messageListener);
                    if (popup && !popup.closed) {
                        popup.close();
                    }
                    const loginAlert = document.getElementById('loginAlert');
                    if (loginAlert) {
                        setAlert(loginAlert, 'danger', event.data.error || 'Đăng nhập Facebook thất bại');
                    }
                }
            };
            
            window.addEventListener('message', messageListener);
            
            // Kiểm tra nếu popup bị đóng thủ công hoặc redirect
            let checkPopup = setInterval(() => {
                if (popup.closed) {
                    clearInterval(checkPopup);
                    window.removeEventListener('message', messageListener);
                    // Không hiển thị thông báo nếu đã nhận được message
                }
            }, 500);
        });
    }

    // ========== REGISTER ==========
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        const registerAlert = document.getElementById('registerAlert');
        const fullnameInput = document.getElementById('fullname');
        const emailInput = document.getElementById('email');
        const phoneInput = document.getElementById('phone');
        
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
                setAlert(registerAlert, 'danger', 'Vui l�ng ki?m tra l?i c�c tru?ng b?t bu?c');
                return;
            }

            const submitBtn = registerForm.querySelector('button[type="submit"]');
            const restoreBtn = disableButton(submitBtn, '<span class="spinner-border spinner-border-sm me-2"></span>�ang dang k�...');

            try {
                const response = await fetch(ENDPOINTS.register, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        fullName,
                        email,
                        phone,
                        
                        password
                    })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    throw new Error(data?.message || 'Kh�ng th? dang k� t�i kho?n');
                }

                setAlert(registerAlert, 'success', data.message || '�ang k� th�nh c�ng');
                registerForm.reset();
                setTimeout(() => {
                    window.location.href = '/User/Login';
                }, 1000);
            } catch (error) {
                console.error('Register error:', error);
                setAlert(registerAlert, 'danger', error.message || '�ang k� th?t b?i');
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
                    emailErr.textContent = 'Vui l�ng nh?p email h?p l?';
                    emailErr.style.display = 'block';
                }
                return;
            }

            const submitBtn = forgotForm.querySelector('button[type="submit"]');
            const restoreBtn = disableButton(submitBtn, '<span class="spinner-border spinner-border-sm me-2"></span>�ang g?i...');

            try {
                const response = await fetch(ENDPOINTS.forgot, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email })
                });

                const data = await parseJsonResponse(response);
                if (!response.ok) {
                    throw new Error(data?.message || 'Kh�ng th? g?i y�u c?u');
                }

                setAlert(successBox, 'success', data.message || 'Vui l�ng ki?m tra email c?a b?n');
                forgotForm.reset();
            } catch (error) {
                console.error('Forgot password error:', error);
                setAlert(forgotError, 'danger', error.message || 'Kh�ng th? x? l� y�u c?u');
            } finally {
                restoreBtn();
            }
        });
    }
})();
