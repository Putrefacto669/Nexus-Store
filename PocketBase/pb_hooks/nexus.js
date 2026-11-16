const productosNombre = "nexus_productos";
const ventasNombre = "nexus_ventas";

function autorizar(e) {
    const clave = $os.getenv("PocketBase__Clave");
    if (!clave || !$security.equal(clave, e.request.header.get("X-Nexus-Key"))) {
        throw new ForbiddenError();
    }
}

function control(app) {
    return app.findRecordById("nexus_estado", "nexusconfig0001");
}

function comprobarImportacion(app) {
    const registro = control(app);
    if (!registro.getBool("importado")) {
        throw new BadRequestError("El almacén de Nexus todavía no está inicializado.");
    }
    return registro;
}

function siguiente(app, registro, campo) {
    const numero = registro.getInt(campo) + 1;
    if (numero > 2147483647) throw new BadRequestError("Se agotó la numeración de registros.");
    registro.set(campo, numero);
    app.save(registro);
    return numero;
}

function productoDto(r) {
    return {
        id: r.getInt("numero"), nombre: r.getString("nombre"), marca: r.getString("marca"),
        estado: r.getInt("estado"), categoria: r.getInt("categoria"),
        precio: r.getInt("precioCentavos") / 100, stock: r.getInt("stock"),
        disponible: r.getBool("disponible"), foto: r.getString("fotoClave") || null
    };
}

function ventaDto(r) {
    return {
        id: r.getInt("numero"), cliente: r.getString("cliente"),
        fechaUtc: r.getString("fechaUtc").replace(" ", "T"),
        cordobasPorDolar: r.getFloat("cordobasPorDolar") || null,
        detalles: JSON.parse(r.getString("detalles"))
    };
}

function crear(app, coleccion) {
    return new Record(app.findCollectionByNameOrId(coleccion));
}

function buscar(app, numero) {
    return app.findRecordsByFilter(productosNombre, "numero = {:numero}", "", 1, 0, { numero })[0];
}

const tiposFoto = { jpg: "image/jpeg", png: "image/png", webp: "image/webp" };

function datosFoto(nombre) {
    const partes = /^([0-9a-f]{32})\.(jpg|png|webp)$/.exec(nombre);
    if (!partes) throw new BadRequestError("Nombre de foto inválido.");
    return tiposFoto[partes[2]];
}

function cargarProducto(r, p, foto) {
    r.load({
        nombre: p.nombre, marca: p.marca, estado: p.estado, categoria: p.categoria,
        precioCentavos: Math.round(p.precio * 100), stock: p.stock, disponible: p.disponible
    });
    if (foto) {
        datosFoto(p.foto);
        r.set("foto", foto);
        r.set("fotoClave", p.foto);
    }
}

function cargarVenta(app, v) {
    const r = crear(app, ventasNombre);
    r.load({ numero: v.id, cliente: v.cliente, fechaUtc: v.fechaUtc,
        cordobasPorDolar: v.cordobasPorDolar || 0, detalles: v.detalles });
    app.save(r);
    return r;
}

function leerFormulario(e) {
    return JSON.parse(e.requestInfo().body.datos);
}

function fotoAdjunta(e, campo) {
    const archivos = e.findUploadedFiles(campo);
    if (archivos.length !== 1) throw new BadRequestError("Debe adjuntarse una foto.");
    return archivos[0];
}

module.exports = {
    estado(e) {
        autorizar(e);
        return e.json(200, { importado: control(e.app).getBool("importado") });
    },
    importar(e) {
        autorizar(e);
        if (control(e.app).getBool("importado")) return e.noContent(204);
        const datos = leerFormulario(e);
        e.app.runInTransaction((tx) => {
            const registro = control(tx);
            if (registro.getBool("importado")) return;
            let ultimoProducto = 0;
            let ultimaVenta = 0;
            for (const p of datos.productos) {
                const r = crear(tx, productosNombre);
                r.set("numero", p.id);
                cargarProducto(r, p, p.foto ? fotoAdjunta(e, "foto" + p.id) : null);
                tx.save(r);
                ultimoProducto = Math.max(ultimoProducto, p.id);
            }
            for (const v of datos.ventas) {
                cargarVenta(tx, v);
                ultimaVenta = Math.max(ultimaVenta, v.id);
                for (const detalle of v.detalles) ultimoProducto = Math.max(ultimoProducto, detalle.productoId);
            }
            registro.load({ ultimoProducto, ultimaVenta, importado: true });
            tx.save(registro);
        });
        return e.noContent(204);
    },
    productos(e) {
        autorizar(e);
        const registros = e.app.findRecordsByFilter(productosNombre, "", "numero", 0, 0) || [];
        return e.json(200, registros.map(productoDto));
    },
    guardarProducto(e) {
        autorizar(e);
        const p = leerFormulario(e);
        const archivo = p.foto ? fotoAdjunta(e, "foto") : null;
        const numero = e.request.pathValue("numero");
        let resultado;
        e.app.runInTransaction((tx) => {
            const registro = comprobarImportacion(tx);
            const r = numero ? buscar(tx, Number(numero)) : crear(tx, productosNombre);
            if (!r) return;
            if (!numero) r.set("numero", siguiente(tx, registro, "ultimoProducto"));
            cargarProducto(r, p, archivo);
            tx.save(r);
            resultado = productoDto(r);
        });
        return resultado ? e.json(200, resultado) : e.noContent(404);
    },
    borrarProducto(e) {
        autorizar(e);
        let eliminado = false;
        e.app.runInTransaction((tx) => {
            comprobarImportacion(tx);
            const r = buscar(tx, Number(e.request.pathValue("numero")));
            if (!r) return;
            tx.delete(r);
            eliminado = true;
        });
        return e.noContent(eliminado ? 204 : 404);
    },
    foto(e) {
        autorizar(e);
        const nombre = e.request.pathValue("nombre");
        const tipo = datosFoto(nombre);
        const fotos = e.app.findRecordsByFilter(productosNombre, "fotoClave = {:nombre}", "", 1, 0, { nombre });
        if (!fotos || !fotos.length) return e.noContent(404);
        const r = fotos[0];
        const fs = e.app.newFilesystem();
        try {
            const lector = fs.getReader(r.baseFilesPath() + "/" + r.getString("foto"));
            try { return e.stream(200, tipo, lector); }
            finally { lector.close(); }
        } finally { fs.close(); }
    },
    ventas(e) {
        autorizar(e);
        const registros = e.app.findRecordsByFilter(ventasNombre, "", "numero", 0, 0) || [];
        return e.json(200, registros.map(ventaDto));
    },
    registrarVenta(e) {
        autorizar(e);
        const datos = e.requestInfo().body;
        const cantidades = new Map();
        if (!Array.isArray(datos.lineas) || datos.lineas.length === 0) {
            return e.json(200, { error: "Agregá al menos un producto." });
        }
        for (const linea of datos.lineas) {
            const total = (cantidades.get(linea.productoId) || 0) + linea.cantidad;
            if (!Number.isInteger(linea.cantidad) || linea.cantidad <= 0 || total > 2147483647) {
                return e.json(200, { error: "La cantidad debe ser mayor que cero y estar dentro del límite permitido." });
            }
            cantidades.set(linea.productoId, total);
        }

        let respuesta;
        e.app.runInTransaction((tx) => {
            const registro = comprobarImportacion(tx);
            const detalles = [];
            const cambios = [];
            for (const [id, cantidad] of cantidades) {
                const r = buscar(tx, id);
                if (!r) { respuesta = { error: "Un producto ya no existe." }; return; }
                const p = productoDto(r);
                if (!p.disponible) { respuesta = { error: "El producto " + p.nombre + " no está disponible." }; return; }
                if (cantidad > p.stock) {
                    respuesta = { error: "Stock insuficiente para " + p.nombre + " (disponible: " + p.stock + ")." };
                    return;
                }
                detalles.push({ productoId: id, productoNombre: p.nombre, cantidad, precioUnitario: p.precio });
                cambios.push({ registro: r, stock: p.stock - cantidad });
            }
            for (const cambio of cambios) {
                cambio.registro.set("stock", cambio.stock);
                tx.save(cambio.registro);
            }
            const venta = cargarVenta(tx, {
                id: siguiente(tx, registro, "ultimaVenta"), cliente: datos.cliente,
                fechaUtc: new Date().toISOString(), cordobasPorDolar: datos.cordobasPorDolar, detalles
            });
            respuesta = { venta: ventaDto(venta) };
        });
        return e.json(200, respuesta);
    }
};
