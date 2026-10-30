/**
 * FitNet - Preloader Cyberpunk HUD Controller
 * Activación EXCLUSIVA en transiciones de Login y Logout
 * La navegación general carga de forma instantánea (0 ms) sin bloqueos ni esperas artificiales.
 */

window.triggerAuthPreloader = function (mainMsg, subMsg) {
    const preloader = document.getElementById('fitnet-preloader');
    if (!preloader) return;

    const percentEl = document.getElementById('preloader-percent-num');
    const progressBar = document.getElementById('preloader-progress-bar');
    const statusMain = preloader.querySelector('.preloader-status-main');
    const statusSub = document.getElementById('preloader-status-sub');

    if (statusMain && mainMsg) statusMain.textContent = mainMsg;
    if (statusSub && subMsg) statusSub.textContent = subMsg;

    preloader.classList.remove('fade-out');
    preloader.classList.add('active');

    let currentPercent = 15;
    if (percentEl) percentEl.textContent = '15';
    if (progressBar) progressBar.style.width = '15%';

    const interval = setInterval(() => {
        currentPercent += 12;
        if (currentPercent >= 98) {
            currentPercent = 98;
            clearInterval(interval);
        }
        if (percentEl) percentEl.textContent = Math.floor(currentPercent);
        if (progressBar) progressBar.style.width = `${Math.floor(currentPercent)}%`;
    }, 40);
};

document.addEventListener('DOMContentLoaded', () => {
    // 1. Conectar con el formulario de Login
    const loginForm = document.querySelector('form[action*="Login"]') || document.querySelector('form[action*="login"]');
    if (loginForm) {
        loginForm.addEventListener('submit', () => {
            const emailInput = loginForm.querySelector('input[name="Email"]') || loginForm.querySelector('#txtEmail');
            const passInput = loginForm.querySelector('input[name="Password"]') || loginForm.querySelector('#txtPassword');
            if (emailInput && emailInput.value && passInput && passInput.value) {
                window.triggerAuthPreloader('// INICIANDO SESIÓN //', '[ VERIFICANDO CREDENCIALES DEL SISTEMA ]');
            }
        });
    }

    // 2. Conectar con los formularios de Logout (en navbar público y dashboard)
    const logoutForms = document.querySelectorAll('form[action*="Logout"], form[action*="logout"]');
    logoutForms.forEach(form => {
        form.addEventListener('submit', () => {
            window.triggerAuthPreloader('// CERRANDO SESIÓN //', '[ DESCONECTANDO CUENTA DE FORMA SEGURA ]');
        });
    });
});
