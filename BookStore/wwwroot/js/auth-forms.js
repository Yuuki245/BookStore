// Toggle functionality for login/register container
document.addEventListener('DOMContentLoaded', function() {
    const container = document.querySelector('.auth-container');
    const registerBtn = document.querySelector('.register-btn');
    const loginBtn = document.querySelector('.login-btn');

    if (container && registerBtn) {
        registerBtn.addEventListener('click', () => {
            container.classList.add('active');
        });
    }

    if (container && loginBtn) {
        loginBtn.addEventListener('click', () => {
            container.classList.remove('active');
        });
    }
});

