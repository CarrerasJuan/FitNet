/**
 * FitNet - Preloader Cyberpunk HUD Controller
 * Anima el porcentaje (0% -> 100%), la barra de progreso Cyberlime
 * y conmuta los estados de inicialización antes de desvanecer suavemente.
 */
document.addEventListener('DOMContentLoaded', () => {
    const preloader = document.getElementById('fitnet-preloader');
    if (!preloader) return;

    const percentEl = document.getElementById('preloader-percent-num');
    const progressBar = document.getElementById('preloader-progress-bar');
    const statusSub = document.getElementById('preloader-status-sub');

    let currentPercent = 0;
    const targetPercent = 100;
    const duration = 1600; // ~1.6 segundos para una carga fluida e impactante
    const intervalTime = 20;
    const increment = targetPercent / (duration / intervalTime);

    const statusMessages = [
        { limit: 25, text: '[ CARGANDO RECURSOS BIOMECÁNICOS ]' },
        { limit: 55, text: '[ SINCRONIZANDO MÓDULOS DE ENTRENAMIENTO ]' },
        { limit: 80, text: '[ OPTIMIZANDO INTERFAZ FITNET ]' },
        { limit: 95, text: '[ VERIFICANDO CREDENCIALES DEL SISTEMA ]' },
        { limit: 100, text: '[ ACCESO CONCEDIDO · BIENVENIDO ]' }
    ];

    const interval = setInterval(() => {
        currentPercent += increment;

        if (currentPercent >= targetPercent) {
            currentPercent = targetPercent;
            clearInterval(interval);

            if (percentEl) percentEl.textContent = '100';
            if (progressBar) progressBar.style.width = '100%';
            if (statusSub) statusSub.textContent = '[ ACCESO CONCEDIDO · BIENVENIDO ]';

            // Pausa breve de 250ms en 100% y desvanecimiento
            setTimeout(() => {
                preloader.classList.add('fade-out');
                setTimeout(() => {
                    preloader.style.display = 'none';
                }, 650);
            }, 250);
        } else {
            const displayVal = Math.floor(currentPercent);
            if (percentEl) percentEl.textContent = displayVal;
            if (progressBar) progressBar.style.width = `${displayVal}%`;

            if (statusSub) {
                const currentMsg = statusMessages.find(m => displayVal <= m.limit);
                if (currentMsg && statusSub.textContent !== currentMsg.text) {
                    statusSub.textContent = currentMsg.text;
                }
            }
        }
    }, intervalTime);
});
