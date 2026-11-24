# Segurança e conformidade

O WEG Inside executa a experiência localmente. Os scripts da experiência usam entrada de mouse e teclado, assets persistentes e renderização local. Essa verificação se refere aos scripts e aos recursos entregues para a experiência.

| Medida implementada | Referência |
| --- | --- |
| Validação das referências ao motor e à câmera, e conferência de dados, renderizadores e colisores de cada peça. A experiência emite um erro descritivo e desativa o controlador em caso de configuração incompleta | `MotorController.Awake`, `MotorPart.Initialize` |
| Validação do controlador do motor e da área 3D antes de vincular a interface | `ExperienceUIController.Start` |
| Seleção restrita a peças registradas no motor e filtragem de colisores de peças invisíveis | `MotorController.Select`, `PartSelectionController.Pick` |
| Gestos bloqueados sobre a interface e cancelados se o aplicativo perder o foco | `ExperienceUIController.BlocksPointer`, `OrbitCameraController.Update`, `OrbitCameraController.OnApplicationFocus` |
| Limites para inclinação, zoom, deslocamento e aproximação da câmera ao volume do modelo | `OrbitCameraController.Update`, `OrbitCameraController.RenderView` |
| Distância de desmontagem e transparência limitadas. Restabelecimento de materiais e estados da interface | `MotorPart.ExplosionOffset`, `MotorStateManager.RequestSeparation`, `XRayController.SetOpacity`, `MotorController.Restore` |
| Bloqueio da rotação durante desmontagem parcial ou completa, com pausa ao mudar de estado | `MotorStateManager.CanRun`, `TryRun` e `RequestSeparation` |
| Faixa de velocidade ilustrativa, normalizada pelo deslizador e limitada no controlador | `MotorRotationController.SetSpeed` e `Advance` |
| Geração restrita ao editor e proteção de cenas com alterações pendentes durante a regeneração | `WegInside.Editor.asmdef`, `SceneSetupGenerator.Generate` |

A cena usa dados didáticos estáticos. Seus scripts são limitados a APIs locais da experiência. Eles não coletam dados pessoais, não solicitam acesso a câmera, microfone ou localização e não iniciam requisições externas. O código da experiência não contém credenciais. As mensagens próprias de diagnóstico descrevem falhas de referência ou configuração.
