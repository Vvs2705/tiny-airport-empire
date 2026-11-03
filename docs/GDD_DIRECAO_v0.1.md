> **Nota do repositório (2026-11-03).** Este é o **documento de direção** do Vinicius para o Little Terminal: diz para onde o jogo vai. Foi copiado, sem alteração, do que está abaixo da linha. **Ele não é o escopo de lançamento.** O que vale para o lançamento está no [`GDD.md`](GDD.md) v1.5: as 9 frentes de gestão no §4.1, as regras de direção no §0.5 e o que fica para depois no §4.4 (roteiro pós-lançamento). Onde os dois divergem, vale o `GDD.md`.

------------------------------------------------------------------------

# LITTLE TERMINAL

## Game Design Document (GDD) --- Documento Mestre de Direção

**Versão:** 0.1 --- Direção de produto e escopo\
**Data:** 03 de novembro de 2026\
**Status:** Documento de alinhamento; sujeito à validação com a
implementação atual\
**Repositório:** `tiny-airport-empire`\
**Nome atual do jogo:** Little Terminal

> **Propósito:** orientar o desenvolvimento de Little Terminal como um
> simulador completo de construção, gestão e operação aeroportuária.
> Este GDD descreve a experiência desejada, os sistemas, as regras de
> design, a direção visual e os critérios de conclusão. Não declara como
> implementado aquilo que ainda está planejado.

------------------------------------------------------------------------

# 0. Como usar este GDD

Este é o documento mestre de direção do produto. Deve orientar game
design, programação, arte, animação, UX/UI, testes e planejamento de
produção.

Para evitar confusão, as informações são separadas em quatro categorias:

-   **Base registrada:** informação identificada nos documentos e na
    estrutura do projeto durante a análise anterior do repositório. Deve
    ser reconfirmada contra a branch `main`.
-   **Direção aprovada:** decisões expressas pelo responsável pelo
    projeto nesta conversa.
-   **Proposta de design:** recomendação para transformar a visão em
    regras jogáveis.
-   **Pendente:** decisão que ainda precisa ser confirmada.

### Direção aprovada

-   Little Terminal será um simulador aeroportuário completo, não apenas
    um tycoon de upgrades rápidos.
-   O jogador será **diretor e operador**: administra o aeroporto e pode
    intervir diretamente nas operações.
-   A simulação terá profundidade elevada, com automação configurável
    para evitar microgerenciamento obrigatório.
-   Arte e animações seguirão uma linguagem **tycoon estilizada**, com
    leitura clara, personagens expressivos e ambientes reconhecíveis.

### Base registrada na análise anterior

A análise anterior do repositório identificou Unity 6000.3.27f1 LTS, C#,
Android como plataforma inicial, orientação retrato, separação entre
regras (`TA.Core`) e apresentação (`TA.View`), simulação com passo fixo,
testes automatizados e sistemas de progressão. Também foram registrados
conteúdos como estações operacionais, perfis de funcionários,
aeroportos, rotas, incidentes, clima, falhas de aeronaves, objetivos,
missões, conquistas e Gems.

Este é um retrato documental anterior, não uma nova auditoria de
execução. Antes de alterar arquitetura ou duplicar sistemas, conferir o
estado atual do código, branches, PRs, cenas e assets. A existência de
uma regra no Core não significa automaticamente que exista uma tela ou
experiência completa para o jogador.

------------------------------------------------------------------------

# 1. Visão do produto

## 1.1 High concept

**Little Terminal é um simulador de construção, gestão e operação
aeroportuária em estilo tycoon.** O jogador começa com uma estrutura
mínima --- hangar, pista simples e uma aeronave --- e transforma essa
instalação em um aeroporto regional, nacional e internacional.

O aeroporto deve parecer um organismo vivo. Passageiros chegam, procuram
informações, fazem check-in, despacham bagagens, passam pela segurança,
compram, aguardam e embarcam. Funcionários cumprem turnos, usam
uniformes próprios, executam tarefas, descansam, recebem treinamento e
podem ser transferidos entre departamentos. Aeronaves pousam, taxi­am,
estacionam, recebem serviços em solo e decolam.

O jogador não administra apenas números. Ele constrói infraestrutura,
dimensiona capacidade, organiza fluxos, toma decisões e responde a
situações operacionais.

## 1.2 Fantasia e objetivo do jogador

"Comecei com um hangar e um avião. Agora sou responsável por um
aeroporto movimentado, com passageiros, equipes especializadas,
companhias aéreas e operações acontecendo ao longo do dia."

O objetivo é construir e operar um aeroporto eficiente, seguro, rentável
e bem avaliado, expandindo sua infraestrutura sem perder o controle dos
fluxos de passageiros, aeronaves, funcionários, bagagens e serviços.

## 1.3 Gênero e apresentação

-   Gênero: tycoon, gestão, construção e simulação.
-   Apresentação: 3D estilizado, câmera isométrica ou três-quartos,
    linguagem visual tycoon.
-   Plataforma inicial registrada: Android.
-   Orientação registrada: retrato.
-   Controles: toque, seleção contextual, painéis e comandos
    operacionais.

------------------------------------------------------------------------

# 2. Pilares de design

1.  **Aeroporto vivo:** personagens, veículos, aeronaves, lojas e
    equipamentos executam comportamentos reconhecíveis.
2.  **Gestão com intervenção direta:** o jogador planeja e delega, mas
    pode selecionar personagens, priorizar tarefas e intervir.
3.  **Sistemas funcionais:** toda instalação importante possui função,
    capacidade, custo, requisitos, operação e consequências.
4.  **Pessoas com rotinas:** passageiros e funcionários possuem estados,
    necessidades, destinos e comportamentos.
5.  **Progressão por complexidade:** cada expansão introduz novos
    fluxos, departamentos, riscos e oportunidades.
6.  **Clareza visual tycoon:** silhuetas, cores, ícones, animações e
    sinalização comunicam o que está acontecendo.
7.  **Profundidade sem microgerenciamento obrigatório:** supervisores,
    regras e prioridades permitem administrar operações maiores.

Uma instalação importante não pode ser apenas decoração. Um raio-X
processa passageiros; um banheiro precisa de limpeza e manutenção; uma
loja possui estoque e atendimento.

------------------------------------------------------------------------

# 3. Papel do jogador e controles

## 3.1 Diretor

O jogador pode: - construir, ampliar e reorganizar instalações; -
contratar, demitir, treinar e promover funcionários; - definir
departamentos, turnos, salários e prioridades; - negociar com companhias
aéreas e fornecedores; - acompanhar voos, passageiros, finanças e
reputação; - comprar equipamentos e materiais; - definir políticas
operacionais; - consultar relatórios.

## 3.2 Operador

O jogador pode: - selecionar funcionário ou equipe; - emitir uma ordem
contextual; - transferir funcionário habilitado; - reforçar uma fila; -
priorizar uma tarefa; - solicitar inspeção ou manutenção; - direcionar
patrulha; - acompanhar ocorrência; - atribuir tarefa temporária; -
cancelar ou substituir ordem, respeitando o estado atual.

O jogador dá a ordem; a IA executa. O personagem não deve
teletransportar-se nem trocar de função instantaneamente.

## 3.3 Hierarquia de ordens

Prioridade recomendada: 1. emergência ou ordem crítica de segurança; 2.
ordem direta do jogador; 3. ordem do supervisor; 4. prioridade do
departamento; 5. rotina automática do funcionário.

Uma ordem não pode violar requisitos de qualificação, segurança,
equipamento ou descanso. Se não puder ser executada, a interface explica
o motivo.

## 3.4 Seleção contextual

Ao tocar em personagem, equipamento ou instalação, mostrar ficha com
nome, função, estado, tarefa atual, destino, necessidades, ações
permitidas e alertas.

------------------------------------------------------------------------

# 4. Público, plataforma e experiência

O público-alvo inclui jogadores de tycoon, construção, logística,
simulação e gestão de equipes. O jogo deve ser acessível a iniciantes e
oferecer camadas de profundidade para jogadores experientes.

A base do projeto foi documentada como Android e orientação retrato. A
experiência deve ser touch-first: - alvos de toque confortáveis; -
painéis curtos e hierarquizados; - informação legível; - zoom e
deslocamento de câmera; - ações frequentes em poucos toques; -
confirmação para ações caras ou destrutivas.

A adequação do formato retrato à visão ampla de um aeroporto deve ser
validada com protótipos. Não mudar a orientação sem teste comparativo e
análise do impacto em cenas e UI.

------------------------------------------------------------------------

# 5. Tempo e ritmo

**Proposta pendente de confirmação:** aeroporto em operação contínua de
24 horas, com ciclo de dia e noite, turnos e voos distribuídos ao longo
do dia.

Velocidades recomendadas: - pausado; - 1x; - 2x; - 3x.

O relógio afeta horários de voos, abertura e fechamento de check-in e
portões, turnos, intervalos, fadiga, limpeza, manutenção, consumo,
estoque, clima, contratos e despesas.

Pausar interrompe o tempo operacional, mas permite construir e consultar
painéis. Tarefas físicas não avançam enquanto pausadas.

O projeto já registrou intenção de progressão offline limitada. A
duração e as atividades permitidas precisam de especificação própria.
Não simular decisões críticas ou incidentes que exijam intervenção
enquanto o jogador estiver ausente.

------------------------------------------------------------------------

# 6. Mundo, câmera e construção

## 6.1 Zonas do aeroporto

1.  **Landside:** acesso público, estacionamento, transporte, entradas e
    terminal antes da segurança.
2.  **Terminal:** check-in, atendimento, lojas, segurança, salas de
    espera, portões e serviços.
3.  **Airside:** pátio, posições de aeronaves, pistas, taxiways e
    equipamentos de solo.
4.  **Apoio:** administração, manutenção, depósitos, bombeiros, polícia,
    canis, carga e infraestrutura.

## 6.2 Câmera

Câmera isométrica/três-quartos, com pan e zoom, mantendo leitura clara
dos espaços. Deve permitir observar a operação sem tornar o controle
difícil em telas pequenas.

## 6.3 Construção

O sistema deve permitir colocar prédios e módulos, construir paredes e
divisórias, posicionar balcões e equipamentos, criar corredores, ampliar
salas, instalar sinalização, definir áreas restritas e remover ou
realocar objetos.

**Proposta:** encaixe em grade ou pontos de ancoragem para estruturas
funcionais, com liberdade controlada para decoração. A solução deve
respeitar o sistema de construção já existente.

Toda instalação funcional precisa de espaço, acesso, conexão com o fluxo
correto, energia/água/rede quando aplicável, equipe habilitada e
manutenção.

------------------------------------------------------------------------

# 7. Direção de arte e animação --- estilo tycoon

## 7.1 Identidade visual

A arte será **3D estilizada em linguagem tycoon**, com aparência
acolhedora, cores organizadas, proporções simplificadas e leitura
imediata. Não é uma simulação fotorealista.

Características: - formas limpas e levemente arredondadas; - volumes
simplificados; - detalhes suficientes para reconhecer equipamentos; -
cores funcionais por setor; - contraste entre piso, paredes, mobiliário
e personagens; - sinalização grande e legível; - iluminação clara; -
efeitos visuais objetivos; - silhuetas distintas; - animações
expressivas e econômicas.

## 7.2 Compatibilidade com a produção existente

A análise anterior registrou abordagem híbrida: ambiente e equipamentos
3D, personagens como sprites 2D pré-renderizados no mundo 3D. Essa
abordagem pode ser mantida se entregar leitura, consistência e
desempenho.

Antes de trocar o pipeline, comparar qualidade visual, custo de
animação, legibilidade em diferentes zooms, desempenho em Android de
entrada e consistência com o cenário. O estilo tycoon não obriga, por si
só, o uso de personagens 2D ou 3D.

## 7.3 Animações essenciais

**Passageiros:** idle, caminhada, caminhada apressada, fila,
sentar/levantar, consultar celular ou painel, usar totem, falar com
atendente, despachar mala, caminhar com mala, embarcar e reações
discretas.

**Atendimento/check-in:** idle no posto, chamar próximo, digitar,
conferir documento, imprimir cartão, etiquetar bagagem e orientar
passageiro.

**Limpeza:** caminhar com carrinho, varrer, passar mop, operar máquina
de piso, recolher lixo, trocar saco, limpar mesa e repor consumíveis.

**Segurança:** orientar fila, operar scanner, inspecionar bandeja,
verificar alarme e encaminhar para inspeção adicional.

**Polícia/K9:** patrulhar, falar por rádio, atender ocorrência, isolar
área, caminhar com cão, cão farejar ponto de inspeção e descansar.

**Pátio:** conduzir veículo, puxar carrinho, abastecer,
carregar/descarregar, operar escada ou ponte, limpar cabine, fazer
manutenção e pushback.

**Aeronaves:** taxiamento, pouso, parada, abertura de portas, conexão de
ponte/escada, embarque, desembarque, pushback e decolagem.

## 7.4 Regras de animação

-   A animação corresponde ao estado lógico da entidade.
-   Não iniciar animação de trabalho se a tarefa não foi aceita.
-   Personagens não atravessam paredes, balcões ou equipamentos.
-   Transições devem ser curtas e legíveis.
-   A animação não é a única confirmação de conclusão: lógica e UI
    também confirmam.
-   Variantes visuais reduzem repetição sem aumentar desnecessariamente
    o custo.

------------------------------------------------------------------------

# 8. Passageiros e jornada

## 8.1 Perfis

Passageiros podem ser viajantes a negócios, turistas, famílias, idosos,
pessoas com mobilidade reduzida, passageiros com crianças, conexões,
VIPs, menores desacompanhados e pessoas que precisam de assistência. Os
perfis alteram necessidades, velocidade, tolerância à espera e serviços
utilizados; devem ser representados com respeito.

## 8.2 Jornada de partida

1.  Chega por transporte, estacionamento ou desembarque.
2.  Consulta voo e terminal.
3.  Faz check-in presencial ou autoatendimento.
4.  Etiqueta e despacha bagagem.
5.  Vai à inspeção.
6.  Passa pela segurança.
7.  Consulta o portão.
8.  Usa lojas e serviços opcionalmente.
9.  Aguarda.
10. Apresenta cartão de embarque.
11. Caminha até a aeronave.
12. Embarca.

## 8.3 Jornada de chegada

1.  Aeronave pousa e estaciona.
2.  Passageiros desembarcam.
3.  Passam pela imigração, quando aplicável.
4.  Seguem à restituição de bagagem.
5.  Retiram malas.
6.  Passam pela alfândega, quando aplicável.
7.  Saem para transporte ou conexão.

## 8.4 Estados e satisfação

Estados possíveis: chegando, procurando informação, em deslocamento, em
fila, em atendimento, despachando bagagem, em inspeção, aguardando
embarque, embarcando, em conexão, retirando bagagem, saindo, atrasado,
perdeu voo ou solicitou assistência.

Satisfação deriva de espera, sinalização, limpeza, conforto,
acessibilidade, atendimento, pontualidade e resolução de problemas. A UI
deve explicar os principais motivos de queda ou melhora.

------------------------------------------------------------------------

# 9. Funcionários e gestão de pessoas

## 9.1 Ficha individual

Cada funcionário possui identificador persistente, nome, aparência,
cargo, departamento, qualificações, experiência, salário, turno, fadiga,
moral, tarefa, uniforme, equipamentos, treinamentos e disponibilidade.

## 9.2 Contratação e desligamento

O jogador consulta candidatos, compara custo e qualificações, contrata e
aloca. Desligamentos atualizam escalas, custos e cobertura de postos.

## 9.3 Transferência entre departamentos

A transferência é uma mecânica central:

1.  jogador seleciona o funcionário;
2.  escolhe novo departamento/função;
3.  sistema valida qualificação, treinamento, vaga, escala e
    necessidade;
4.  se faltar qualificação, oferece treinamento ou bloqueia;
5.  funcionário conclui a tarefa atual ou faz passagem de serviço;
6.  desloca-se ao vestiário;
7.  troca uniforme e equipamentos;
8.  registra a mudança;
9.  caminha ao novo posto;
10. inicia a nova rotina.

A transferência não é instantânea. O tempo de preparação deve ser
visível.

## 9.4 Qualificações

Exemplos: atendimento, check-in, operação de totem, bagagem, inspeção,
limpeza, manutenção elétrica/predial, operação de solo, condução de
veículo, primeiros socorros e supervisão.

Um funcionário pode ter várias qualificações, mas atua somente em
funções para as quais esteja habilitado e equipado.

## 9.5 Turnos, fadiga e supervisores

O sistema considera entrada, saída, intervalos, folgas, turnos
diurnos/noturnos, fadiga, recuperação e ausência. Posto crítico sem
funcionário habilitado gera alerta.

Supervisores distribuem tarefas e cobrem necessidades dentro de limites
definidos pelo jogador. Não tomam decisões estratégicas ou financeiras
em seu lugar.

## 9.6 Uniformes

Uniformes identificam departamento e função. Mudanças visuais ocorrem
durante a rotina de transferência, no vestiário, com equipamentos de
proteção exigidos pela função.

------------------------------------------------------------------------

# 10. Departamentos e instalações

Cada departamento deve possuir espaço físico, função, capacidade,
equipe, equipamentos, custos, consumíveis quando aplicável, indicadores
e consequências.

  -----------------------------------------------------------------------
  Departamento                        Instalações e responsabilidades
  ----------------------------------- -----------------------------------
  Administração/Operações             escritório, central de operações,
                                      contratos, orçamento, coordenação

  Atendimento                         guichês, informações, achados e
                                      perdidos, assistência e reclamações

  Autoatendimento                     totens de voos, check-in, cartões,
                                      bagagem, mapas e serviços

  Check-in/Bagagens                   balcões, balanças, etiquetas,
                                      esteiras, triagem, carga e
                                      restituição

  Segurança                           filas, bandejas, detectores,
                                      scanners, inspeção adicional e
                                      controle de acesso

  Embarque                            portões, leitores, filas, equipe,
                                      ligação com ponte ou escada

  Limpeza                             carrinhos, depósitos, máquinas,
                                      banheiros, áreas públicas e
                                      reposição

  Comércio                            lojas, cafés, restaurantes,
                                      quiosques, máquinas e contratos

  Pátio                               posições, veículos, bagagem,
                                      abastecimento, catering, água,
                                      resíduos e pushback

  Pista/Operações aéreas              pista, taxiways, iluminação,
                                      sinalização, inspeção e manutenção

  Polícia                             posto, patrulhas, ocorrências,
                                      apoio e isolamento de áreas

  Unidade K9                          cães, condutores, canil, descanso,
                                      alimentação, saúde e treinamento

  Bombeiros                           posto, veículos, equipe de
                                      prontidão e resposta

  Saúde                               posto médico, profissionais,
                                      ambulância e atendimento

  Manutenção                          elétrica, hidráulica, climatização,
                                      elevadores, esteiras, veículos e
                                      pista

  Carga                               armazéns, docas, triagem,
                                      empilhadeiras e contratos

  Transporte/Estacionamento           vagas, cancelas, totens, táxis,
                                      ônibus e locadoras
  -----------------------------------------------------------------------

## 10.1 Segurança e K9

A polícia atende ocorrências, patrulha e apoia passageiros. A unidade K9
é formada por cão e condutor qualificado. Cada cão possui saúde,
energia, condicionamento, experiência e especialização. Precisa de
canil, descanso, água, alimentação, cuidados veterinários, treinamento e
escala.

As atividades K9 devem ser apresentadas de forma segura e abstrata,
focadas em patrulha e inspeção simulada; não reproduzir procedimentos
sensíveis do mundo real.

## 10.2 Venda de passagens

Guichês presenciais devem oferecer atendimento e emissão/consulta
conforme o modelo comercial. A venda também pode ocorrer por sistemas
das companhias aéreas. A regra exata --- quem vende a passagem e quem
recebe a receita --- precisa ser definida na economia do jogo.

------------------------------------------------------------------------

# 11. Aeronaves, voos e companhias

Cada voo possui identificador, companhia, origem/destino, horários,
aeronave, capacidade, portão/posição, estado e quantidade de
passageiros/bagagens.

Estados possíveis: programado, aguardando aeronave, aproximação,
pousando, taxiando, estacionado, desembarque, atendimento em solo,
check-in/embarque, embarque encerrado, pushback, decolado, atrasado,
cancelado ou desviado.

Ciclo de atendimento: 1. aproximação e pouso; 2. taxiamento e
estacionamento; 3. desembarque; 4. descarga de bagagens; 5. limpeza e
serviços; 6. abastecimento, quando necessário; 7. catering, água e
resíduos, conforme operação; 8. carregamento de bagagens; 9. embarque;
10. fechamento de portas; 11. pushback e saída.

Companhias possuem identidade, frota, rotas, frequência, capacidade e
requisitos. Contratos dependem de infraestrutura, pontualidade,
qualidade e volume. A torre e o controle de tráfego são uma camada de
gestão simplificada, sem exigir simulação profissional de controle
aéreo.

------------------------------------------------------------------------

# 12. Economia

**Receitas:** taxas aeroportuárias, serviços a aeronaves, contratos,
aluguel de lojas, participação em vendas, estacionamento, carga,
hangaragem, publicidade e serviços opcionais.

**Despesas:** salários, energia, água, manutenção, limpeza, materiais,
combustível de veículos, treinamento, contratos, seguros, construção e
reposição.

Relatórios diários, semanais e mensais devem mostrar receita, despesa,
lucro/prejuízo, custo por departamento, receita por companhia, receita
comercial, custo de pessoal, atrasos e manutenção pendente.

Princípios: - expansão exige planejamento financeiro; - contratar sem
demanda pode gerar prejuízo; - equipe insuficiente gera filas e
atrasos; - eficiência melhora reputação e atratividade; - melhorias
possuem custo, benefício e manutenção; - nenhum sistema essencial deve
exigir pagamento premium.

------------------------------------------------------------------------

# 13. Progressão

Os aeroportos já registrados no projeto --- Vale Verde, Costa Azul e
Aurora Global --- podem servir como marcos da campanha, sujeitos à
validação do conteúdo atual.

1.  **Hangar inicial:** hangar, pista simples, avião pequeno, escritório
    e operação mínima.
2.  **Aeroporto local:** terminal básico, atendimento, check-in, sala de
    espera, banheiro, limpeza e primeiro portão.
3.  **Aeroporto regional:** totens, bagagem, segurança, lojas, mais
    voos, supervisores e manutenção.
4.  **Aeroporto nacional:** mais companhias, pátio ampliado, polícia,
    K9, saúde, bombeiros, carga e transporte.
5.  **Aeroporto internacional:** imigração, alfândega, conexões,
    terminal internacional e duty free.
6.  **Hub internacional:** múltiplos terminais, alto volume, centro de
    operações e logística avançada.

Desbloqueios devem depender de marcos operacionais, reputação,
capacidade, finanças e objetivos --- não apenas de uma barra de
experiência.

------------------------------------------------------------------------

# 14. Incidentes e eventos

Exemplos: falta de funcionário, fila excessiva, totem fora de serviço,
esteira parada, bagagem não localizada, passageiro que perdeu voo,
solicitação de assistência, conflito, objeto abandonado, falha de
energia, problema hidráulico, equipamento de pista indisponível,
aeronave com problema, chuva/neblina, atraso/cancelamento e emergência
médica.

Cada incidente possui gatilho, gravidade, setor responsável, opções de
resposta, recursos, tempo de resolução, consequências e registro
histórico.

Eventos críticos devem explicar o que aconteceu e por que a resposta foi
eficaz ou insuficiente. A simulação de segurança deve permanecer
abstrata e orientada à gestão.

------------------------------------------------------------------------

# 15. UX/UI

## HUD principal

Dinheiro, hora/velocidade, reputação, alertas, fluxo/ocupação e acesso à
construção e aos painéis.

## Painel de operações

Chegadas, partidas, atrasos, portões, aeronaves em solo, tarefas
críticas e áreas congestionadas.

## Painel de funcionários

Total por departamento, em serviço, em pausa, ausentes, vagas,
qualificações, turnos e cobertura.

## Painel de passageiros

Passageiros no terminal, filas, espera, conexões, assistência e
satisfação.

## Painel de instalações

Capacidade, estado, manutenção, consumo, equipe, custo e eficiência.

## Painel financeiro

Receitas, despesas, lucro, caixa, contratos, projeções e custos por
departamento.

Alertas devem ser classificados como críticos, importantes, informativos
ou relatórios. Todo alerta deve abrir o local, personagem, voo ou
instalação relacionada.

------------------------------------------------------------------------

# 16. IA e simulação escalável

Passageiros combinam destino, rota, estado da jornada, fila,
necessidades, tolerância à espera e horário limite. Funcionários
combinam função, qualificação, turno, tarefa, prioridade, local, fadiga,
equipamento e ordens.

A navegação respeita paredes, portas, balcões, mobiliário, áreas
restritas, filas e obstáculos. Se um destino ficar inacessível, a IA
procura rota alternativa ou informa bloqueio. Personagens não atravessam
objetos sólidos.

Para desempenho: - personagens próximos recebem simulação detalhada; -
fluxos distantes podem usar atualização agregada; - tarefas são
atualizadas em frequências adequadas; - animação e lógica são
desacopladas; - entidades fora da área visível não precisam executar
detalhes visuais; - resultados agregados preservam regras econômicas e
operacionais.

Otimização não pode fazer passageiros desaparecerem de filas, concluir
voos sem recursos ou ignorar falta de funcionários.

------------------------------------------------------------------------

# 17. Arquitetura, dados e salvamento

Preservar a separação entre regras de simulação e apresentação
identificada na base do projeto, caso continue válida:

-   **Core:** regras, estados, tempo, economia, tarefas, passageiros,
    voos, funcionários e salvamento.
-   **View:** cenas, modelos, animações, UI, efeitos e câmera.
-   **Integração:** adaptadores entre eventos do Core e elementos
    visuais.

A apresentação não é a fonte de verdade. A animação representa uma
tarefa validada pelo Core.

Se o passo fixo e a simulação determinística registrados forem mantidos,
novos sistemas devem respeitar ticks, ordenação estável de eventos,
aleatoriedade controlada, testes reproduzíveis e salvamento consistente.

O save deve preservar aeroporto, construções, funcionários,
qualificações, escalas, voos, contratos, economia, inventário, progresso
e eventos relevantes. Migrações de schema precisam ser testadas. Custos,
tempos, capacidades, salários e parâmetros de equilíbrio devem ser
configuráveis por dados sempre que possível.

Cada sistema precisa de testes unitários, integração entre
departamentos, cenários de simulação, save/load, desempenho e validação
visual em dispositivo.

------------------------------------------------------------------------

# 18. Desempenho, acessibilidade e qualidade

Metas de desempenho precisam ser validadas em aparelhos Android reais,
não apenas no Editor ou PC. Controlar CPU de navegação, memória de
modelos/texturas/animações, atualizações de UI, carregamento e
salvamento. Usar LOD e pooling quando apropriado.

Acessibilidade: - texto legível em tela pequena; - ícones acompanhados
de rótulos quando necessário; - informação não comunicada apenas por
cor; - controles de volume e vibração; - velocidade configurável; -
alertas com explicação de causa; - tutoriais contextuais; - opção para
reduzir efeitos intensos; - localização preparada para idiomas
adicionais.

------------------------------------------------------------------------

# 19. Monetização e integridade

A documentação anterior registrou intenção de jogo gratuito com anúncios
recompensados, Gems, cosméticos e passe. O modelo comercial deve ser
reconfirmado antes de fechar escopo.

Princípios: - monetização opcional; - nenhuma compra obrigatória para
concluir a campanha; - não vender vantagem operacional essencial; - não
usar caixas pagas aleatórias; - anúncios recompensados voluntários; -
compras validadas antes da concessão; - transparência de preço e
conteúdo; - progressão principal equilibrada sem pagamento.

A loja não deve ser priorizada antes de o ciclo operacional principal
estar divertido e estável.

------------------------------------------------------------------------

# 20. Prioridades de produção

## P0 --- Núcleo obrigatório

Construção inicial; hangar, pista e aeronave; passageiro com jornada
completa; voo completo; terminal básico; check-in; embarque/desembarque;
funcionário individual; tarefas e navegação; contratação; transferência;
troca de uniforme; limpeza; filas; painel de operações; economia básica;
save/load.

## P1 --- Aeroporto regional

Totens; informações; bagagens; segurança; lojas; escalas; treinamento;
supervisores; manutenção; companhias e rotas; incidentes; reputação e
relatórios.

## P2 --- Serviços especializados

Polícia; K9; bombeiros; saúde; estacionamento avançado; transporte
terrestre; carga aérea; manutenção avançada; contratos especializados.

## P3 --- Internacional e escala

Imigração; alfândega; conexões internacionais; múltiplos terminais;
operações de grande escala e automação avançada.

A ordem pode ser ajustada conforme arquitetura, dependências e testes.
Não iniciar muitos sistemas P2/P3 enquanto o fluxo P0 estiver
incompleto.

------------------------------------------------------------------------

# 21. Vertical slice --- primeira experiência completa

A primeira entrega de referência é um pequeno aeroporto que funciona de
ponta a ponta.

O jogador começa com hangar, pista e avião; constrói atendimento; recebe
passageiros; realiza check-in; processa bagagens; organiza sala de
espera; faz embarque e envia o avião.

Deve incluir: - ambiente tycoon estilizado; - câmera e controles; -
construção básica; - pelo menos um voo completo; - passageiros
visíveis; - funcionários com rotinas; - contratação e alocação; -
transferência de funcionário; - troca de uniforme; - limpeza; - filas; -
painel de operações; - receita e despesa; - save/load.

**Critério:** o jogador observa e compreende uma operação completa sem
depender de sistemas fictícios ou botões sem função.

------------------------------------------------------------------------

# 22. Critérios de conclusão de funcionalidades

Uma funcionalidade só pode ser marcada como concluída quando: 1. regra
de gameplay implementada; 2. interface de consulta/controle, quando
necessária; 3. representação visual coerente; 4. animação ou feedback
adequado; 5. requisitos e custos definidos; 6. integração com
departamentos relacionados; 7. estado salvo e restaurado; 8. testes das
regras principais; 9. ausência de bloqueios conhecidos de navegação; 10.
validação em build e, quando relevante, dispositivo Android.

**Exemplo --- banheiro:** não está concluído apenas porque o modelo 3D
foi colocado. Precisa de capacidade, acesso, consumo, sujeira, limpeza,
manutenção, impacto na satisfação, UI e persistência.

**Exemplo --- funcionário de limpeza:** não está concluído apenas porque
existe um personagem com uniforme. Precisa ser contratado, escalado,
receber tarefa, encontrar o local, limpar, atualizar o ambiente e
responder a ordens.

------------------------------------------------------------------------

# 23. Indicadores de sucesso

Indicadores propostos para playtests: - jogador entende o fluxo de
passageiros sem consultar manual constantemente; - é possível
identificar por que uma fila cresce; - transferência de funcionários
ajuda, mas respeita tempo e qualificação; - limpeza é visível e afeta o
aeroporto; - jogador percebe diferença entre administrar e intervir; -
expansão cria novas decisões, não apenas mais cliques; - automação reduz
microgerenciamento sem retirar controle; - jogador entende por que um
voo atrasou; - aeroporto opera coerentemente em diferentes
velocidades; - desempenho é aceitável nos dispositivos-alvo.

------------------------------------------------------------------------

# 24. Riscos e mitigação

-   **Escopo excessivo:** priorizar P0 e exigir vertical slice
    funcional.
-   **Simulação cara:** usar níveis de detalhe, atualização por eventos
    e testes de carga.
-   **Microgerenciamento cansativo:** oferecer supervisores, prioridades
    e automações.
-   **Sistemas visuais sem regra:** aplicar critérios de conclusão.
-   **Divergência entre GDD e código:** manter changelog e verificar
    `main` antes de grandes decisões.
-   **Problemas de navegação:** testar rotas, detectar bloqueios e
    registrar falhas.
-   **Dependências externas:** planejar separadamente publicação, SDKs,
    compras, anúncios, licenças e privacidade.

------------------------------------------------------------------------

# 25. Decisões pendentes

Não tratar como aprovadas até confirmação: 1. tempo contínuo 24 horas ou
dias de operação; 2. velocidades finais; 3. manter retrato ou avaliar
paisagem; 4. personagens 2D pré-renderizados ou 3D estilizados; 5.
construção em grade, modular ou livre controlada; 6. nível de controle
direto sobre tarefas; 7. profundidade de torre, pista e tráfego; 8.
departamentos do primeiro lançamento versus atualizações; 9. modelo
comercial final; 10. escopo da simulação offline.

------------------------------------------------------------------------

# 26. Glossário

-   **Landside:** área pública, antes do controle de segurança.
-   **Airside:** área operacional restrita, incluindo pátio e pistas.
-   **Check-in:** confirmação do passageiro para o voo.
-   **Gate/Portão:** ponto de controle de embarque.
-   **Turnaround:** tarefas realizadas na aeronave entre chegada e
    próxima partida.
-   **Pushback:** retirada da aeronave da posição com veículo
    apropriado.
-   **K9:** unidade operacional formada por cão e condutor.
-   **GDD:** documento que descreve visão, regras, sistemas e
    experiência.
-   **Vertical slice:** pequena parte do jogo que demonstra a
    experiência final com sistemas integrados.
-   **Core:** camada de regras e simulação.
-   **View:** camada visual, interface, animação e apresentação.
-   **LOD:** nível de detalhe usado para adaptar custo visual ou de
    simulação.

------------------------------------------------------------------------

# 27. Regra final de direção

Little Terminal deve ser desenvolvido como um **simulador aeroportuário
tycoon, com operação viva e gestão profunda**.

O jogador começa pequeno, aprende a executar e organizar operações,
contrata pessoas, constrói setores, delega responsabilidades e
transforma a instalação em um aeroporto complexo.

O jogo não precisa simular cada detalhe técnico da aviação real. Precisa
simular de forma consistente as atividades que tornam um aeroporto
reconhecível: pessoas, fluxos, horários, equipes, aeronaves, bagagens,
serviços, segurança, manutenção e decisões.

**Regra que orienta toda a produção: se o jogador constrói algo que
parece operacional, aquilo precisa ter uma função real na simulação ---
ou ser claramente identificado como decoração.**
