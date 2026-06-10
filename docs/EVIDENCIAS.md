# Evidencias de ejecución (Nitro / HTTP)

API en `http://localhost:5095/graphql`. Esquema generado por Hot Chocolate 16 con
*mutation conventions* activas. Respuestas reales capturadas contra la BD `lab08`.

> Nota de esquema: con `--use-database-names` la PK es `productID` (no `productId`).
> Con *mutation conventions*, `addProduct` devuelve `{ productPayload { ... } }` y
> `updatePrice` devuelve `{ product, errors }`. El tipo de error es `ProductNotFoundError`.

---

## P1 — Productos: paginación + orden + proyección + campo `category`
```graphql
query {
  products(first: 5, order: { listPrice: DESC }) {
    nodes { productID name listPrice category }
    pageInfo { hasNextPage endCursor }
  }
}
```
Respuesta (extracto):
```json
{ "data": { "products": {
  "nodes": [
    { "productID": 749, "name": "Road-150 Red, 62", "listPrice": 3578.27, "category": "Bikes" },
    { "productID": 750, "name": "Road-150 Red, 44", "listPrice": 3578.27, "category": "Bikes" }
  ],
  "pageInfo": { "hasNextPage": true, "endCursor": "NA==" }
} } }
```

## P2 — Filtro por nombre `contains "Mountain"`
```graphql
query { products(where: { name: { contains: "Mountain" } }, first: 3) {
  nodes { productID name listPrice } } }
```
Respuesta: `Mountain End Caps (328)`, `LL Mountain Rim (507)`, `ML Mountain Rim (508)`.

## P3 — Cliente con sus órdenes (relación anidada)
```graphql
query {
  customers(first: 3, where: { salesOrderHeaders: { some: { totalDue: { gt: 0 } } } }) {
    nodes { customerID salesOrderHeaders { salesOrderID orderDate totalDue } }
  }
}
```
Respuesta (extracto): cliente `11000` con órdenes `43793 (3756.99)`, `51522 (2587.88)`,
`57418 (2770.27)`, etc.

## P4 — Mutación: crear producto
```graphql
mutation {
  addProduct(input: { name: "Casco SENA Edition", productNumber: "SENA-227547", listPrice: 89.99 }) {
    productPayload { productId name listPrice }
  }
}
```
Respuesta:
```json
{ "data": { "addProduct": { "productPayload": { "productId": 1000, "name": "Casco SENA Edition", "listPrice": 89.99 } } } }
```

## P5 — Validación (error controlado)
```graphql
mutation { addProduct(input: { name: "X", productNumber: "X-NEG-1", listPrice: -1 }) {
  productPayload { productId } } }
```
Respuesta:
```json
{ "errors": [ { "message": "El precio no puede ser negativo.", "path": ["addProduct"] } ], "data": null }
```

## P6 — Suscripción `onProductAdded` (prueba integradora, WebSocket)
Suscripción activa:
```graphql
subscription { onProductAdded { productId name listPrice } }
```
Al ejecutar `addProduct` en otra sesión, el suscriptor recibe en tiempo real:
```json
{ "id": "1", "type": "next", "payload": { "data": { "onProductAdded":
  { "productId": 1001, "name": "Producto Suscripcion", "listPrice": 42.50 } } } }
```

## P7 — `updatePrice` con error tipado + Escenario C (`onPriceChanged`)
Caso id inexistente (error tipado):
```graphql
mutation {
  updatePrice(input: { id: 999999, newPrice: 10.0 }) {
    product { productID name listPrice }
    errors { __typename ... on ProductNotFoundError { message } }
  }
}
```
Respuesta:
```json
{ "data": { "updatePrice": { "product": null,
  "errors": [ { "__typename": "ProductNotFoundError", "message": "No existe el producto con id 999999." } ] } } }
```
Caso id real (`1000`): `product` actualizado a `55.55`, `errors: null`, y el suscriptor de
`onPriceChanged` recibe el evento en tiempo real:
```json
{ "payload": { "data": { "onPriceChanged":
  { "productId": 1000, "name": "Casco SENA Edition", "listPrice": 55.55 } } } }
```

## Escenario B — `topCustomers(year)` (BFF / panel de ventas)
```graphql
query { topCustomers(year: 2024, first: 5) { nodes { customerId totalAmount orderCount } } }
```
Respuesta (extracto): `29641 → 475912.56 (3 órdenes)`, `29913 → 470230.99 (4)`, …
> Los datos de `lab08` abarcan los años **2022–2025** (no 2013).
