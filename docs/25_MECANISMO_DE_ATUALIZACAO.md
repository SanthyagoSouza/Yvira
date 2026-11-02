# 25 - Mecanismo de Atualização da Linguagem (Dev & Produção)

Para manter o Yvira sempre atualizado na máquina do desenvolvedor e também permitir atualizações automáticas via comando do CLI em máquinas de usuários finais, implementamos o **pipeline de duas vias**:

---

## 1. Como você atualiza a linguagem no seu computador após criar novas features

Sempre que você alterar o código do compilador, VM ou adicionar novas funcionalidades e quiser que o comando `yvira` no terminal já reflita as mudanças instantaneamente, criamos um script automatizado:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish_local.ps1
```

### O que o script faz sozinho:
1. Compila todos os projetos da solução (`Release`).
2. Gera o binário nativo single-file (`dist\bin\Yvira.Cli.exe`).
3. Atualiza o alias global `dist\bin\yvira.exe` no PATH.
4. Avisa no console quando estiver pronto.

Qualquer alteração feita no código agora passa a valer para o comando global `yvira` em 1 clique!

---

## 2. Como os usuários finais atualizam o Yvira (`yvira upgrade`)

No CLI foi adicionado o comando:

```powershell
yvira upgrade
```

### Como ele funciona:
1. Consulta a URL de releases oficial do Yvira (`version.json`).
2. Compara a versão instalada localmente com a última lançada.
3. Se houver nova versão:
   - Baixa os bytes do executável atualizado (`yvira.exe.update`).
   - Renomeia o executável atual para `.old` (técnica do Windows para atualizar executáveis em execução).
   - Substitui pelo novo `yvira.exe`.
   - Exibe mensagem de sucesso.
4. Se o usuário estiver offline ou em ambiente de desenvolvimento local, exibe a instrução para usar o `publish_local.ps1`.
