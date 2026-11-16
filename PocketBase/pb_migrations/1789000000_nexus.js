migrate((app) => {
    const productos = new Collection({
        type: "base",
        name: "nexus_productos",
        fields: [
            { name: "numero", type: "number", onlyInt: true, min: 1, required: true },
            { name: "nombre", type: "text", max: 80, required: true },
            { name: "marca", type: "text", max: 60 },
            { name: "estado", type: "number", onlyInt: true, min: 0, max: 2 },
            { name: "categoria", type: "number", onlyInt: true, min: 0, max: 4 },
            { name: "precioCentavos", type: "number", onlyInt: true, min: 1, max: 9999999900, required: true },
            { name: "stock", type: "number", onlyInt: true, min: 0, max: 1000000 },
            { name: "disponible", type: "bool" },
            { name: "fotoClave", type: "text", max: 37 },
            { name: "foto", type: "file", maxSelect: 1, maxSize: 3145728, protected: true,
                mimeTypes: ["image/jpeg", "image/png", "image/webp"] }
        ],
        indexes: [
            "CREATE UNIQUE INDEX idx_nexus_producto_numero ON nexus_productos (numero)",
            "CREATE UNIQUE INDEX idx_nexus_producto_foto ON nexus_productos (fotoClave) WHERE fotoClave != ''"
        ]
    });
    const ventas = new Collection({
        type: "base",
        name: "nexus_ventas",
        fields: [
            { name: "numero", type: "number", onlyInt: true, min: 1, required: true },
            { name: "fechaUtc", type: "date", required: true },
            { name: "cliente", type: "text", max: 120, required: true },
            { name: "cordobasPorDolar", type: "number", min: 0 },
            { name: "detalles", type: "json", required: true }
        ],
        indexes: ["CREATE UNIQUE INDEX idx_nexus_venta_numero ON nexus_ventas (numero)"]
    });
    const estado = new Collection({
        type: "base",
        name: "nexus_estado",
        fields: [
            { name: "importado", type: "bool" },
            { name: "ultimoProducto", type: "number", min: 0, onlyInt: true },
            { name: "ultimaVenta", type: "number", min: 0, onlyInt: true }
        ]
    });
    app.save(productos);
    app.save(ventas);
    app.save(estado);
    const control = new Record(estado);
    control.id = "nexusconfig0001";
    app.save(control);
}, (app) => {
    for (const nombre of ["nexus_estado", "nexus_ventas", "nexus_productos"]) {
        app.delete(app.findCollectionByNameOrId(nombre));
    }
});
