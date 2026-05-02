---
description: Full-stack yangi feature — backend + frontend boshidan oxirigacha
---

Feature: $ARGUMENTS

## Bosqich 1 — Rejalashtirish
`planner` agentni chaqir:
- Feature ni tahlil qil
- Backend + frontend uchun faza-faza reja tuz
- Foydalanuvchiga ko'rsat va tasdiq so'ra

## Bosqich 2 — Backend
Foydalanuvchi tasdiqlagan bo'lsa:
`backend-implementer` agentni chaqir:
- Tasdiqlangan reja bo'yicha backend yoz
- Tartib: Domain → Infrastructure → Application → Api
- Har qadamdan keyin `dotnet build` ishga tushir

## Bosqich 3 — Frontend
`frontend-implementer` agentni chaqir:
- Backend tayyor bo'lgach frontend yoz
- Tartib: Types → Service → Hook → Components → Page
- Har qadamdan keyin `npm run build` ishga tushir

## Bosqich 4 — Review
`reviewer` agentni chaqir:
- Backend va frontend ikkalasini tekshir
- Muammolar bo'lsa — tegishli implementer ga qaytар
- Hamma narsa yaxshi bo'lsa — yakuniy hisobot ber