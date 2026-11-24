# Testes e evidências

Validação executada em 24/11/2026 com Unity 6000.6.8f1, URP 17.6.0, Input System 1.20.1 e Windows x64. As suítes foram executadas no editor conectado pelo Unity MCP.

## Resultados

| Verificação | Resultado observado |
| --- | --- |
| Compilação dos scripts | Zero erros e zero avisos na recompilação final |
| Edit Mode | 4 de 4 testes passaram |
| Play Mode | 5 de 5 testes passaram em duas execuções consecutivas após o ajuste do teste de ponteiro |
| Cena e prefab após reabrir o Unity | Cena ativa e salva, 11 peças com dados persistentes, 47 malhas renderizadas com materiais persistentes, 23 malhas com pré-processamento de colisão habilitado e nenhum script ausente na hierarquia |
| UI Toolkit em 16:10 | Game View de 872 × 545. Rodapé dentro do painel e centros de todas as peças explodidas dentro da área 3D, com raio-X e desmontagem ativos em conjunto |
| Build Windows x64 | `StandaloneWindows64` com `MotorExperience` como única cena. Build `build_0d640fb1d540` concluída com zero erros e zero avisos |
| Inicialização do executável | Player iniciado localmente em modo batch por 7 segundos. O Input System e o dispositivo gráfico inicializaram e o processo se manteve ativo até o encerramento pelo teste |
| Console após reabrir | Zero erros e zero avisos |

Os testes de Edit Mode cobrem limites e transições da desmontagem, bloqueio da simulação, restauração e persistência das peças, materiais, malhas e configurações de colisão. Os de Play Mode carregam a cena salva e cobrem clique e arraste com mouse virtual, zoom, deslocamento, atalhos, seleção pela lista e por raycast, informações educativas, foco, isolamento, raio-X, rotação sincronizada, remontagem e restauração da experiência.

## Revisões e correções

- A primeira compilação acusou uma referência ausente ao URP no código do editor. A referência explícita na definição de assembly do gerador resolveu o erro.
- A revisão visual ajustou o espaçamento axial, o giro inicial da câmera na desmontagem, a placa didática, o fundo e a iluminação. A nova captura mostrou as peças dianteiras e traseiras com espaço entre elas.
- A primeira build Windows acusou 23 malhas sem a opção de pré-processamento de colisão. A configuração foi adicionada à geração dos assets e testada no prefab antes da build final. [API de pré-processamento de colisão](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/physicseditormeshextensions/setprebakecollisionmesh)
- Uma execução do teste virtual perdeu o clique na lista após mover o ponteiro e pressionar o botão no mesmo evento. A sequência passou a enviar um evento de movimento antes do clique e foi repetida com sucesso.

## Renderização e desempenho

O prefab contém **133.514 triângulos** em 47 malhas de renderização. No Game View a **2560 × 1440**, a cena montada registrou **60,2 FPS médios** em 120 quadros consecutivos por `Time.unscaledDeltaTime`, com **Intel Core i5-14500** e **NVIDIA T400 4 GB**. É uma amostra no editor. O desempenho prolongado do executável e de outras GPUs permanece sem medição.

- [Motor montado](Evidencias/01-montado.png)
- [Vista explodida com estator selecionado](Evidencias/02-explodido.png)
- [Raio-X durante a simulação](Evidencias/03-raio-x.png)
- [Interface 16:10 com vista explodida e raio-X](Evidencias/04-painel-16x10.png)

## Escopo da verificação do Player

A build de validação foi gerada em `Temp/WEGInsideWindows`, uma pasta temporária do editor. O log de inicialização incluiu um diagnóstico do Direct3D 12 ao consultar a interface opcional de mensagens. A execução continuou até o encerramento pelo teste. A revisão visual e a interação foram verificadas no Game View. Permanecem pendentes uma sessão interativa e um teste prolongado do executável em outra máquina Windows.
