const search = document.getElementById('productSearch');

if (search) {
    const cards = document.querySelectorAll('[data-product]');
    const empty = document.getElementById('noSearchResults');
    const normalize = text => text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase('es');

    search.addEventListener('input', () => {
        const query = normalize(search.value.trim()).split(/\s+/);
        let visible = 0;
        for (const card of cards) {
            const product = normalize(card.dataset.product);
            card.hidden = !query.every(word => product.includes(word));
            if (!card.hidden) visible++;
        }
        empty.hidden = visible > 0;
    });
}
