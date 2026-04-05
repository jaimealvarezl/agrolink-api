# Plan de Implementacion del Modulo Clinical Assistant (Telegram) en AgroLink API

## 1. Objetivo y alcance

Este plan adapta el `PLAN.md` original al stack y arquitectura reales de este repositorio.

Objetivo v1:

- Implementar un modulo de asistente clinico para Telegram dentro de esta solucion .NET.
- Mantener lineamientos actuales: Clean Architecture, CQRS con MediatR, EF Core y pruebas.
- Mantener identificadores internos en ingles y respuestas al usuario final en espanol rural simple.

Decisiones confirmadas:

- No se validaran permisos de usuario para este modulo clinico.
- La resolucion del caso sera por granja mencionada + animal mencionado en el mensaje.
- Se reutilizaran los modelos existentes (`Farm`, `Animal`) y se agregaran solo modelos clinicos complementarios.
- Mientras no exista catalogo de medicinas, en esta primera fase se habilita modo de sugerencia directa por IA para pruebas.

## 2. Analisis del plan original

### Fortalezas del plan original

- Flujo end-to-end claro: audio entrante -> STT -> extraccion estructurada -> reglas deterministicas -> respuesta saliente.
- Enfoque de seguridad adecuado (`pending_confirmation`, manejo de datos faltantes, alertas por riesgo).
- Estrategia de costo enfocada en modelos mini.
- Buena trazabilidad con logs y estados.

### Ajustes necesarios para este proyecto

- El plan original usa `Node.js + PostgreSQL`; este repo usa `.NET + EF Core + MediatR`.
- No se validaran permisos por usuario en este modulo; la seguridad principal sera integridad de webhook e idempotencia.
- `ear_tag` debe mapearse al modelo actual de animal:
  - Propuesta principal: usar `Animal.TagVisual` como identificador externo.
  - Propuesta alternativa: crear `ClinicalEarTag` si negocio exige semantica mas estricta y unicidad por granja.
- Ademas de `ear_tag`, se debe soportar busqueda de animal por menciones de `TagVisual`, `Cuia` o `Name` cuando aplique.
- El webhook de Telegram debe ser asincrono: responder rapido y procesar en segundo plano.
- Las salidas a Telegram deben ser idempotentes para evitar duplicados por reintentos.

## 3. Arquitectura propuesta en AgroLink

### API Layer

Nuevos controllers:

- `TelegramWebhookController`
  - `POST /api/integrations/telegram/webhook`
- `ClinicalCatalogController`
  - `POST /api/clinical/catalog/import`
- `ClinicalCasesController`
  - `GET /api/clinical/animals/{earTag}/latest-report`
  - `GET /api/clinical/cases/{caseId}`

Notas:

- El webhook puede ser `[AllowAnonymous]`, pero debe validar secreto/firma de Telegram.
- No se realizara validacion de permisos por usuario en los flujos clinicos de Telegram.

### Application Layer (Vertical Slices)

Crear feature:

- `src/AgroLink.Application/Features/ClinicalCases/...`

Slices sugeridos:

- `Commands/ReceiveTelegramUpdate`
- `Commands/ProcessClinicalEvent`
- `Commands/ConfirmCaseLink`
- `Commands/ImportMedicationCatalog`
- `Queries/GetLatestAnimalClinicalReport`
- `Queries/GetClinicalCaseById`

Interfaces sugeridas:

- `ITranscriptionService`
- `IClinicalExtractionService`
- `IClinicalRulesEngine`
- `IClinicalSafetyGate`
- `ITextToSpeechService`
- `ITelegramGateway`
- `IClinicalCaseLinker`
- `IFarmAnimalResolver`
- `IClinicalMedicationAdvisorService` (modo temporal sin catalogo)

### Domain Layer

Nuevas entidades:

- `ClinicalCase`
- `ClinicalCaseEvent`
- `ClinicalRecommendation`
- `Medication`
- `MedicationRule`
- `MedicationImage`
- `ClinicalAlert`
- `TelegramOutboundMessage`
- `TelegramInboundEventLog`

Nuevos enums:

- `ClinicalCaseState`: `NewCase`, `AwaitingRequiredData`, `PendingConfirmation`, `Recommended`, `Alerted`, `Closed`
- `ClinicalRiskLevel`: `Low`, `Medium`, `High`, `Critical`
- `ClinicalMessageIntent`: `NewCaseReport`, `FollowUpReport`, `AnimalStatusRequest`, `ConfirmationReply`
- `ExtractionConfidenceLevel`: `High`, `Medium`, `Low`
- `RecommendationSource`: `RulesEngine`, `AiExploratory`

### Infrastructure Layer

- Extender `AgroLinkDbContext` con nuevos `DbSet<>`.
- Agregar configuraciones EF en `Data/Configurations`.
- Agregar repositorios y sus interfaces.
- Implementar servicios externos:
  - `OpenAiTranscriptionService`
  - `OpenAiClinicalExtractionService`
  - `OpenAiTextToSpeechService`
  - `TelegramGateway`
- Registrar dependencias en `Infrastructure/DependencyInjection.cs`.

## 4. Modelo de datos propuesto (v1)

### Relaciones principales

- `ClinicalCase`:
  - `FarmId` requerido.
  - `AnimalId` nullable al inicio, obligatorio antes de recomendar dosis.
  - `FarmReferenceText` para trazar como se detecto la granja mencionada.
  - `AnimalReferenceText` para trazar como se detecto el animal mencionado.
  - `EarTag` requerido como identificador entrante.
- `ClinicalCaseEvent` pertenece a `ClinicalCase`.
- `ClinicalRecommendation` pertenece a `ClinicalCase` y `Medication`.
- `ClinicalAlert` pertenece a `ClinicalCase`.
- `TelegramOutboundMessage` puede apuntar a `ClinicalCase` (nullable para respuestas de estado).

### Decisiones de almacenamiento

- Guardar raw JSON de cada update de Telegram para auditoria/replay.
- Guardar transcript y extraccion estructurada en `jsonb`.
- Guardar confidence y metadata de modelo por evento.
- Crear clave unica para idempotencia de salida.
- Guardar `RecommendationSource` y disclaimer mostrado al usuario.

### Indices requeridos

- `ClinicalCase (FarmId, EarTag, OpenedAt DESC)`
- `ClinicalCaseEvent (CaseId, CreatedAt DESC)`
- `ClinicalRecommendation (CaseId, CreatedAt DESC)`
- `MedicationRule (Species, Active)`
- `TelegramInboundEventLog (TelegramUpdateId)` unico

## 5. Flujos funcionales

### Flujo A: caso nuevo por nota de voz

1. Llega update de webhook, se persiste y se responde rapido.
2. Un proceso en background descarga audio y ejecuta STT.
3. Se extrae JSON estructurado con schema estricto.
4. Se resuelve granja mencionada y animal mencionado (orden sugerido: `TagVisual` -> `Cuia` -> `Name`).
5. Se validan campos clinicos requeridos.
6. Si faltan datos criticos: `AwaitingRequiredData` y pregunta de seguimiento por audio.
7. Si hay datos suficientes:
   - Sin catalogo: usar `AiExploratory` para sugerencias temporales de prueba.
   - Con catalogo: usar reglas deterministicas + safety gate.
8. Se envia respuesta `texto + audio + imagen de medicamento`.

### Flujo B: seguimiento dentro de 7 dias

1. Buscar caso abierto por `FarmId + AnimalId` (o `EarTag` como fallback) y ventana de 7 dias.
2. Si la continuidad es clara, agregar evento y recalcular recomendacion.
3. Si es ambiguo, poner `PendingConfirmation` y pedir confirmacion.
4. No recomendar dosis hasta confirmar continuidad.

### Flujo C: consulta de estado por animal

1. Detectar intent `AnimalStatusRequest` con granja mencionada + animal mencionado.
2. Obtener ultimo caso/recomendacion.
3. Responder resumen en `texto + audio` y adjuntar imagen si aplica.
4. Si no hay historial, devolver respuesta controlada.

### Safety

- Riesgo `High/Critical` o confidence bajo -> crear `ClinicalAlert`.
- Mensaje conservador a grupo y posible escalamiento a revisor.
- La fuente de verdad de dosis es siempre el rules engine.
- En modo `AiExploratory`, bloquear instrucciones absolutas y exigir mensaje de validacion con veterinario.

### Modo temporal sin catalogo (primera fase)

Objetivo:
- Permitir pruebas funcionales del flujo clinico antes de tener catalogo propio de medicamentos.

Comportamiento:
- La IA puede sugerir opciones de medicamentos veterinarios de forma orientativa.
- El prompt debe indicar contexto de Nicaragua.
- La respuesta debe incluir donde buscar/comprar en Nicaragua (tipos de lugar y canales).
- Siempre incluir advertencia de que no sustituye diagnostico veterinario.

Prompt base sugerido (modo `AiExploratory`):

```text
Actua como asistente clinico veterinario orientativo para ganado bovino.
Contexto geografico obligatorio: Nicaragua.
Con base en los sintomas transcritos, sugiere opciones de medicamentos veterinarios posibles para evaluacion inicial.
No des ordenes absolutas ni diagnostico definitivo.
Incluye:
1) opciones de medicamento (nombre generico y, si aplica, nombre comercial comun),
2) para que casos suele usarse cada opcion,
3) precauciones y contraindicaciones basicas,
4) que informacion falta para decidir tratamiento seguro,
5) donde buscarlo en Nicaragua (veterinarias agropecuarias locales, distribuidores veterinarios, farmacias veterinarias, cooperativas ganaderas, y consulta con veterinario colegiado local).
Respuesta en espanol simple y corta.
Termina con advertencia: "Validar siempre con un medico veterinario en Nicaragua antes de aplicar cualquier tratamiento."
```

## 6. Plan de implementacion por fases

## Fase 0 - Descubrimiento y contratos

Entregables:

- Definir modo exacto de integracion Telegram (webhook bot, grupos, secreto).
- Definir estrategia de resolucion de granja mencionada y animal mencionado (normalizacion y desempate).
- Definir schema estricto de extraccion.
- Definir prompt y formato de salida para modo temporal `AiExploratory` (sin catalogo).
- Definir formato de reglas de medicamentos v1.

Criterio de salida:

- Contratos aprobados con ejemplos de payload.

## Fase 1 - Fundacion de dominio y persistencia

Entregables:

- Entidades y enums nuevos.
- Configuraciones EF + migracion.
- Interfaces y repositorios basicos.
- Soporte de persistencia para `RecommendationSource` y disclaimer.

Criterio de salida:

- Migracion aplicada sin errores.
- Pruebas base de repositorio en verde.

## Fase 2 - Pipeline de ingest

Entregables:

- Webhook con validacion de firma/secreto e idempotencia.
- Command `ReceiveTelegramUpdate` y orquestacion async.
- Descarga de audio desde Telegram.

Criterio de salida:

- Duplicados de webhook no duplican procesamiento.
- Evento entrante llega al pipeline.

## Fase 3 - Extraccion AI y linking de casos

Entregables:

- Servicios de STT/extraccion via adaptadores OpenAI.
- `CaseLinker` para `new`, `follow_up` y `pending_confirmation`.
- `FarmAnimalResolver` para mapear texto entrante a `Farm` y `Animal` existentes.
- Validador de datos obligatorios.

Criterio de salida:

- Extraccion estructurada persistida con confidence.
- Transiciones de estado correctas.

## Fase 4 - Reglas, safety y composicion de respuesta

Entregables:

- Implementacion del rules engine deterministico.
- Implementacion de safety gate.
- Composer de respuesta y TTS.
- Resolver de imagen de medicamento.
- Plan de switch controlado de `AiExploratory` -> `RulesEngine` cuando exista catalogo.

Criterio de salida:

- Recomendacion consistente y trazable.
- Casos de alto riesgo y baja confianza generan alertas.

## Fase 5 - Consulta de estado e import de catalogo

Entregables:

- Queries `latest-report` y `case by id`.
- Importador CSV + manifest de imagenes.
- Reporte de filas invalidas.

Criterio de salida:

- Catalogo usable en recomendaciones.
- Consulta de estado funciona para `earTag` conocido/desconocido.

## Fase 6 - Hardening

Entregables:

- Observabilidad: logs estructurados, metricas de latencia y fallas.
- Reintentos y estrategia para fallas no recuperables.
- Revision de seguridad (webhook, secretos, PII, prompt injection).

Criterio de salida:

- Checklist de confiabilidad y seguridad aprobado.

## 7. Estrategia de pruebas

### Unit tests

- Heuristicas del case linker.
- Validador de datos requeridos.
- Reglas de dosis del rules engine.
- Decisiones del safety gate.

### Application tests

- Handlers de comandos de ingest/procesamiento/confirmacion/import.
- Handlers de queries de reporte y detalle de caso.

### Integration tests

- Webhook -> procesamiento end-to-end con Telegram/OpenAI mock.
- Idempotencia bajo reintentos.
- Resolucion correcta de granja/animal con casos ambiguos y no ambiguos.

### Criterios minimos de aceptacion

- Audio valido completo -> recomendacion con texto/audio/imagen.
- Sin catalogo -> respuesta orientativa de IA con contexto Nicaragua y seccion de donde buscar medicamento.
- Datos criticos faltantes -> no dosis, se pide aclaracion.
- Seguimiento ambiguo -> `PendingConfirmation`, sin dosificacion.
- Riesgo alto -> alerta + respuesta conservadora.
- `earTag` sin historial -> respuesta controlada de no registro.

## 8. Riesgos principales y mitigaciones

- Riesgo: mala asignacion de granja o animal por ambiguedad del texto.
  - Mitigacion: resolver por jerarquia (`TagVisual` -> `Cuia` -> `Name`) + confirmacion explicita en casos ambiguos.
- Riesgo: costos por prompts largos.
  - Mitigacion: schema estricto, contexto minimo y limites de tokens.
- Riesgo: duplicados por retries de Telegram.
  - Mitigacion: idempotencia en inbound y outbound.
- Riesgo: recomendaciones inseguras por baja confianza.
  - Mitigacion: safety gate obligatorio y bloqueo de dosis.
- Riesgo: sugerencias incorrectas en modo sin catalogo.
  - Mitigacion: marcar salida como orientativa, pedir validacion veterinaria local y limitar a fase temporal de pruebas.

## 9. Definition of Done del modulo

- Estructura vertical slice implementada en `Features/ClinicalCases`.
- Migraciones y configuraciones EF aplicadas correctamente.
- `dotnet build agrolink-api.sln` en verde.
- `dotnet test` en verde.
- Escenarios criticos cubiertos por pruebas automatizadas.
- Documentacion API actualizada con contratos y ejemplos.
