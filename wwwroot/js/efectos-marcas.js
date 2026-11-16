(() => {
    const boton = document.querySelector('[data-reproducir-efecto]');
    const efecto = document.querySelector('[data-efecto-marca]');

    if (boton && efecto) {
        let ocultarEfecto;
        boton.hidden = false;
        boton.addEventListener('click', () => {
            efecto.classList.remove('en-reproduccion');
            // Reinicia la animación aunque el usuario vuelva a pulsar mientras se reproduce.
            void efecto.offsetWidth;
            efecto.classList.add('en-reproduccion', 'efecto-activado');
            clearTimeout(ocultarEfecto);
            ocultarEfecto = setTimeout(() => efecto.classList.remove('efecto-activado'), 2200);
        });
    }

    const etapa = document.querySelector('[data-comparacion-rtx]');
    const control = document.querySelector('[data-control-rtx]');
    if (etapa && control) {
        const divisor = control.querySelector('input');
        function cambiarComparacion() {
            const original = divisor.valueAsNumber;
            etapa.style.setProperty('--divisor', original + '%');
            etapa.dataset.vistaRtx = original === 0 ? 'on' : original === 100 ? 'off' : 'ambas';
            divisor.setAttribute('aria-valuetext', original + '% original, ' + (100 - original) + '% con RTX');
        }
        control.hidden = false;
        divisor.addEventListener('input', cambiarComparacion);
        cambiarComparacion();
    }
})();
