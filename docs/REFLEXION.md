# Respuestas a las preguntas de reflexión

## ¿Cuándo preferir REST sobre GraphQL?
- **Recursos simples y cacheables vía HTTP/CDN:** REST aprovecha el caché HTTP por URL/método
  (ETag, `Cache-Control`), algo que GraphQL (un único `POST /graphql`) no obtiene gratis.
- **Carga/descarga de archivos** y respuestas binarias, donde el modelo de recursos de REST
  encaja mejor.
- **Contratos muy estables y públicos** consumidos por equipos heterogéneos: el contrato
  REST + OpenAPI es directo y ampliamente soportado.
- **Equipos o clientes que no necesitan flexibilidad de consulta:** si la forma de los datos
  es fija, la flexibilidad de GraphQL es complejidad innecesaria.

GraphQL brilla cuando el cliente necesita **componer la forma exacta** de los datos en una
sola petición (p. ej. el panel BFF del Escenario B: clientes + agregados de ventas del año
en una llamada, que en REST serían varias o un endpoint ad-hoc).

## ¿Qué cambia al activar `[UseProjection]`?
- **Sin proyección:** EF materializa la **entidad completa** (todas las columnas) y, si se
  navegan relaciones, puede disparar cargas adicionales (N+1) o requerir `Include`.
- **Con proyección:** Hot Chocolate traduce la selección GraphQL a un `SELECT` que pide
  **solo las columnas consultadas**. Menos I/O, menos memoria y menos riesgo de N+1.
- Evidencia: pedir `{ productID name listPrice }` genera
  `SELECT [ProductID],[Name],[ListPrice],[ProductSubcategoryID] FROM Product` y no las ~25
  columnas de la tabla (ver `COMO_SE_EVITO_EL_N+1.md`).

## ¿Riesgos de las consultas profundas y cómo se mitigan?
- **Explosión combinatoria de joins/resolvers:** relaciones anidadas (cliente→órdenes→
  detalles→producto→…) pueden multiplicar el trabajo exponencialmente.
- **DoS por complejidad:** un cliente malicioso puede enviar una consulta muy profunda o
  amplia para saturar el servidor.
- **Mitigaciones aplicadas:**
  - `AddMaxExecutionDepthRule(8)` limita la profundidad de la consulta.
  - El análisis de **costo/complejidad** de Hot Chocolate 16 (directiva `@cost`, activo por
    defecto) rechaza operaciones que superan un presupuesto.
  - `[UsePaging(MaxPageSize = ...)]` acota el tamaño de página.
  - En producción se puede desactivar la introspección y usar *persisted queries*.
