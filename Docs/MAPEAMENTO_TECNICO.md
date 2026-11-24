# Mapeamento técnico

O WEG Inside apresenta um motor de indução trifásico em um estúdio virtual. A demonstração para Windows é operada com mouse e teclado. O Figma é o destino do protótipo visual da proposta mobile.

| Função | Protótipo Unity para Windows | Proposta mobile |
| --- | --- | --- |
| Apresentação do equipamento | Prefab 3D procedural de um motor inspirado no W22 com materiais URP e iluminação de estúdio | Conceito de sobreposição de um modelo virtual junto ao equipamento físico |
| Enquadramento | Câmera orbital virtual com zoom, deslocamento, foco e ajuste à vista explodida | Avaliação futura de enquadramento pela câmera do dispositivo e gestos de toque |
| Seleção | Raycast em colisores, botões na lista e painel com dados educativos | Conceito de seleção por toque em elementos virtuais |
| Desmontagem, isolamento e raio-X | Posições interpoladas sem física, isolamento por peça, opacidade da estrutura externa e exposição do rotor | Demonstração visual dos comandos e estados prevista no Figma |
| Simulação | Movimento sincronizado de rotor, eixo e ventilador, condicionado à montagem | Conceito de controle da simulação sobre o modelo virtual |
| Relação com o espaço físico | Cena virtual autocontida | Câmera, ancoragem e rastreamento do ambiente pertencem à evolução da proposta mobile |

O desktop executa toda a interação em coordenadas do Unity e utiliza o Input System para mouse e teclado. A referência à realidade aumentada descreve a proposta futura. O projeto desktop contém apenas a cena virtual e seus controles.

## Modelo e recursos persistentes

- `Motor_W22.prefab` reúne 11 peças com pivôs no eixo longitudinal **X**. Rolamentos e tampas têm peças frontal e traseira independentes. Parafusos e arruelas acompanham a tampa em que estão montados.
- `MotorMeshBuilder` gera perfis de revolução ocos, superfícies chanfradas, tubos, furações verdadeiras nos pés e pás curvas. `MotorModelGenerator` agrupa geometrias por peça e material e persiste os recursos em `Assets/Meshes`, `Assets/Materials` e `Assets/Data`.
- Cada `MotorPart` no prefab referencia um `MotorPartData` com identificador, nome, grupo, categoria, descrição, função e importância. O vínculo está no componente anexado ao GameObject para que o mesmo dado possa ser usado por outras instâncias do prefab.
- A caixa de ligação tem a tampa levantada durante a separação ou o isolamento, permitindo examinar os terminais. O eixo central se eleva na vista explodida para preservar a leitura das demais peças coaxiais.
- A placa de identificação usa o texto didático **W22 DIDATICO** modelado na própria geometria. [Referência visual da linha W22](https://static.weg.net/medias/downloadcenter/hf8/h29/WEG-w22-three-phase-electric-motor-50029265-brochure-english-web.pdf)

## Estados e apresentação

`MotorStateManager` calcula o estado de desmontagem e restringe a rotação ao estado montado. `MotorController` aplica as transições, publica mudanças para a interface e coordena transparência, isolamento e seleção. `OrbitCameraController` usa a área livre entre os painéis como referência de enquadramento.

O UI Toolkit separa layout, tema e eventos em UXML, USS e componentes C#. `SceneSetupGenerator` configura o documento, os materiais do estúdio e a câmera. [Painéis do UI Toolkit no Unity 6.6](https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/uielements/uie-support-for-runtime-ui/uie-render-runtime-ui/uie-runtime-panel-settings)
