// forget-password.html logic
if (document.getElementById('forgotForm')) {
    document.getElementById('forgotForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const emailInput = document.getElementById('email');
        const emailErr = document.getElementById('emailErr');
        const successBox = document.getElementById('successBox');

        const email = emailInput.value.trim();
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        emailErr.style.display = 'none';
        successBox.style.display = 'none';
        emailInput.classList.remove('is-invalid');

        if (!re.test(email)) {
            emailErr.style.display = 'block';
            emailInput.classList.add('is-invalid');
            return;
        }

        setTimeout(() => {
            successBox.style.display = 'block';
        }, 600);
    });
}

// login.html logic
if (document.getElementById('loginForm')) {
    document.getElementById('togglePwd').addEventListener('click', function () {
        const p = document.getElementById('password');
        const icon = document.getElementById('eyeIcon');

        if (p.type === 'password') {
            p.type = 'text';
            icon.classList.replace('bi-eye-slash', 'bi-eye');
        } else {
            p.type = 'password';
            icon.classList.replace('bi-eye', 'bi-eye-slash');
        }
    });

    document.getElementById('loginForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const email = document.getElementById('email');
        const pwd = document.getElementById('password');
        const emailHelp = document.getElementById('emailHelp');

        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!re.test(email.value.trim())) {
            email.classList.add('input-error');
            emailHelp.style.display = 'block';
            return;
        } else {
            email.classList.remove('input-error');
            emailHelp.style.display = 'none';
        }

        alert('Đăng nhập thành công (mô phỏng).');
    });
}

// register.html logic
if (document.getElementById('registerForm')) {
    function togglePwd(btnId, inputId, iconId) {
        const btn = document.getElementById(btnId);
        if (!btn) return;
        btn.addEventListener('click', function () {
            const input = document.getElementById(inputId);
            const icon = document.getElementById(iconId);
            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.replace('bi-eye-slash', 'bi-eye');
            } else {
                input.type = 'password';
                icon.classList.replace('bi-eye', 'bi-eye-slash');
            }
        });
    }
    togglePwd('togglePwd', 'password', 'eyeIcon');
    togglePwd('togglePwd2', 'confirmPwd', 'eyeIcon2');

    document.getElementById('registerForm').addEventListener('submit', function (e) {
        e.preventDefault();
        const form = this;

        Array.from(form.querySelectorAll('.is-invalid')).forEach(el => el.classList.remove('is-invalid'));

        const fullname = document.getElementById('fullname');
        const email = document.getElementById('email');
        const phone = document.getElementById('phone');
        const pwd = document.getElementById('password');
        const confirm = document.getElementById('confirmPwd');
        const agree = document.getElementById('agree');

        let ok = true;

        if (!fullname.value.trim()) {
            fullname.classList.add('is-invalid'); ok = false;
        }

        const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRe.test(email.value.trim())) {
            email.classList.add('is-invalid'); ok = false;
        }

        const phoneRe = /^[0-9]{9,15}$/;
        if (!phoneRe.test(phone.value.trim())) {
            phone.classList.add('is-invalid'); ok = false;
        }

        const pwdRe = /^(?=.*[A-Za-z])(?=.*\d).{6,}$/;
        if (!pwdRe.test(pwd.value)) {
            pwd.classList.add('is-invalid'); ok = false;
        }

        if (pwd.value !== confirm.value || !confirm.value) {
            confirm.classList.add('is-invalid'); ok = false;
        }

        if (!agree.checked) {
            agree.classList.add('is-invalid'); ok = false;
        }

        if (!ok) return;

        alert('Đăng ký thành công (mô phỏng). Xin chào ' + fullname.value.trim() + '!');
        form.reset();
    });
}