// Cliente HTTP da API. A URL base pode ser sobrescrita em build com VITE_API_URL.
export const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

// TODO: implemente as chamadas do contrato de API (ver README na raiz, seção "CONTRATO DE API"):
//   POST   /leads/consultation
//   PUT    /leads/{id}/steps/consultation
//   POST   /leads/{id}/steps/simulation
//   PATCH  /leads/{id}/steps/simulation/{simulationId}/select
//   PUT    /leads/{id}/steps/identification
//   PUT    /leads/{id}/steps/professional-banking-data
//   POST   /leads/{id}/documents
//   GET    /leads/{id}/documents
//   DELETE /leads/{id}/documents/{documentId}
//   POST   /leads/{id}/confirm
//   POST   /leads/{id}/retry-submission
//   GET    /leads
//   GET    /leads/{id}
