# WEG Inside — Por dentro das máquinas

Protótipo 3D interativo para uma demonstração educativa no Centro WEG. A experiência permite explorar a montagem de um motor de indução trifásico, identificar suas peças e observar como rotor, eixo e ventilador se movem em conjunto.

O modelo procedural usa a arquitetura da linha WEG W22 como referência visual. As dimensões, tolerâncias e velocidades são ilustrativas. [Catálogo técnico da linha W22](https://static.weg.net/medias/downloadcenter/hf8/h29/WEG-w22-three-phase-electric-motor-50029265-brochure-english-web.pdf)

## Executar no Unity

1. Abra esta pasta pelo Unity Hub com **Unity 6000.6.8f1**.
2. Abra a cena [MotorExperience](Assets/Scenes/MotorExperience.unity).
3. Pressione **Play** e use o mouse e o teclado na aba **Game**.

A cena utiliza cinco objetos de nível raiz. `Motor_W22` é a instância do prefab, **Estúdio industrial** contém o piso e as três luzes, **Câmera orbital** enquadra o modelo, **Controles da experiência** gerencia os estados e **Interface WEG Inside** contém o UI Document.

## Controles

| Interação | Ação |
| --- | --- |
| Arrastar com botão esquerdo sobre o fundo | Orbitar o motor |
| Roda do mouse | Aproximar ou afastar |
| Arrastar com botão direito na área 3D | Deslocar o enquadramento |
| Clicar na peça ou em um item de **Componentes** | Selecionar e ler as informações da peça |
| **Focalizar componente** ou **Isolar componente** | Aproximar a câmera da peça ou exibir somente a peça selecionada |
| **Separar peças**, **Remontar motor** ou deslizador | Animar a montagem ou escolher um ponto entre 0% e 100% |
| **Modo raio-X** | Ajustar a transparência. **Ocultar carcaça** mostra as peças internas sem as superfícies externas. **Expor rotor** libera a visão bloqueada pelo estator |
| **Iniciar motor**, **Pausar motor** ou **Reiniciar simulação** | Controlar a rotação sincronizada do motor montado. Reiniciar pausa e retorna ao ângulo inicial |
| **R** / **Esc** | Restaurar a câmera / limpar a seleção |
| **Restaurar visualização** | Parar a simulação, remontar o motor, limpar a seleção e retornar à vista inicial |

## Estrutura

| Caminho | Conteúdo |
| --- | --- |
| `Assets/Scenes` | Cena da experiência |
| `Assets/Prefabs/Motor_W22.prefab` | Motor reutilizável com 11 peças selecionáveis em nove grupos |
| `Assets/Meshes`, `Assets/Materials`, `Assets/Data` | Malhas persistentes, materiais URP e dados educativos por peça |
| `Assets/Scripts/{Core,Camera,Motor,Interaction,UI}` | Estados, entrada, animações procedurais, seleção e interface |
| `Assets/Scripts/Editor` | Geração do modelo, materiais e cena pelo menu **WEG Inside → Gerar cena e motor** |
| `Assets/UI` | Layout UXML, tema e estilos USS, painel responsivo |
| `Assets/Tests` | Testes de Edit Mode e Play Mode com Unity Test Framework |
| `Docs` | Mapeamento técnico, controles implementados e evidências dos testes |

O projeto usa C#, URP 17.6, Input System 1.20.1, UI Toolkit e Unity Test Framework. A cena e as malhas geradas são assets salvos. Use o menu de geração quando precisar reconstruí-los a partir dos scripts. A geração atualiza os assets da experiência e sobrescreve sua cena e seu prefab.

## Gerar o executável Windows

1. Confira no Unity Hub se o Editor tem suporte a **Windows Build Support**.
2. No Unity, abra **File → Build Profiles** e selecione **Windows** com arquitetura **Intel 64-bit**.
3. Na lista de cenas do perfil, mantenha `Assets/Scenes/MotorExperience.unity` habilitada como primeira cena. Para distribuir apenas a experiência, limite a lista a essa cena.
4. Clique em **Build**, escolha uma pasta fora de `Assets` e aguarde a geração.
5. Abra o `.exe` na máquina Windows. Para copiar a entrega, mantenha o executável e os arquivos e diretórios auxiliares da build juntos.

## Limitações

- A rotação é cinemática e a velocidade é apresentada como percentual ilustrativo.
- Dimensões, folgas e acabamentos atendem à explicação visual. Ensaios de desempenho do motor, cálculo eletromagnético e dimensionamento mecânico estão fora do escopo.
- O estator opaco encobre o rotor na montagem. Use **Expor rotor**, a desmontagem ou o isolamento para vê-lo.
- A interface foi projetada para janelas de desktop. A proposta mobile em realidade aumentada pertence ao protótipo visual separado no Figma.

[Mapeamento técnico](Docs/MAPEAMENTO_TECNICO.md) · [Segurança e conformidade](Docs/SEGURANCA_CONFORMIDADE.md) · [Testes e capturas](Docs/TESTES.md)
