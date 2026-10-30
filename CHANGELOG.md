# Changelog — Tiny Airport Empire

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões em [SemVer](https://semver.org/lang/pt-BR/).

## [Unreleased]
### Adicionado
- Fase C2 no núcleo, 2026-10-30 (`docs/CONTRATO_FASE_C2.md`, `docs/BALANCE.md` §6): as 8 estações e os 6 perfis por desbloqueio de marco. Bagagem e alimentação são paradas opcionais que pagam; a oficina divide o tempo da checagem e da inspeção; o despacho aumenta o bônus de pontualidade. A especialização ficou só visual pelo portão do §5.13. Save no schema 3, com migração testada num save real do schema 2. TA-1: Bom 99,7%, Lento 75,4%. TA-2: 1,408. O começo da v0.1 não mudou.
- Tripo, 2026-10-30: Lotes 8, 9 e 10 gerados e exportados na conta nova (66 peças: Vale Verde rural, pátio e carga, interior, terreno, economia, 15 personagens com rig, incidentes de equipamento, eventos e fila). Saldo no fim do dia: 16 440. `docs/INTEGRACAO_ARTE.md` lista as peças com uso e rota para a etapa de integração; proveniência em `docs/PROVENIENCIA.md` §2.5.
- Tripo, 2026-10-30, conta nova (25 200 créditos): as 6 peças presas do Lote 6 regeradas e o Lote 7 inteiro gerado e exportado (76 peças: terminais n3, 3 aeronaves, 16 personagens com rig, Costa Azul, Aurora, incidentes, interior e extras). Arte crua em `arte/tripo/` fora do git; proveniência em `docs/PROVENIENCIA.md` §2.4 e lista em `docs/ASSETS.md` §2.6.
- Tripo, 2026-10-29, fim do dia: o plano Max venceu às ~17:10 com 6 318 créditos sem uso. O Lote 7 (76 peças, 4 804 créditos) ficou só planejado em `docs/ASSETS.md` §2.6. Do Lote 6, 5 de 11 exports estão guardados e 6 estão bloqueados pelo plano gratuito (`docs/PROVENIENCIA.md` §1 e §2.3).
- GDD v1.2 (2026-10-29), com o padrão de mercado:
  - tripulação de pilotos e comissários (§5.11);
  - pátio e manutenção (§5.12);
  - mundo vivo e mapa que cresce (§5.13);
  - rota de arte híbrida (D-18);
  - benchmark dos prints e do trailer de referência em `docs/COMPETITIVO.md` §g, com o diferencial TA-8.
- GDD mestre "Little Terminal" v1.1 (2026-10-29) em `docs/GDD.md`: a v1.0 do Vinicius revisada com a auditoria do repositório (NO-GO para 29/10), a reconciliação com as regras do estúdio e as decisões pendentes D-01 a D-17. O GDD anterior virou `docs/GDD_ORIGEM.md`.
- Repositório criado (2026-10-09) com o GDD revisado (comparativo de mercado no topo), `docs/COMPETITIVO.md` e a estrutura de pastas do projeto Unity.
