const precio = document.getElementById('Precio');
const dolares = document.getElementById('precioDolares');
const formato = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function actualizarPrecio() {
    const valor = precio.valueAsNumber;
    dolares.textContent = Number.isFinite(valor)
        ? 'US$ ' + formato.format(valor / Number(dolares.dataset.tasa))
        : '—';
}

precio.addEventListener('input', actualizarPrecio);
actualizarPrecio();
