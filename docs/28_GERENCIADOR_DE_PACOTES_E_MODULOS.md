# 28 - Gerenciador de Pacotes e Módulos Remotos (yvira install)

O Yvira conta com um sistema moderno de gerenciamento de dependências e pacotes sem a necessidade de registros centralizados complexos, seguindo as melhores práticas da Web moderna:

1. **Manifesto `yvira.json`**: Definição declarativa simples de dependências.
2. **Modelo Híbrido de Armazenamento**: Cache global centralizado (`~/.yvira/cache/`) para economia de disco e downloads instantâneos, com espelhamento transparente na pasta local do projeto (`.yvira/packages/`).
3. **Módulos Remotos e Descentralizados**: Suporte a repositórios GitHub (`github:usuario/repo`) e URLs diretas (`https://.../modulo.yv`).

---

## 1. Estrutura do Manifesto `yvira.json`

O arquivo `yvira.json` na raiz do seu projeto registra as dependências do ecossistema:

```json
{
  "name": "meu_projeto_ia",
  "version": "1.0.0",
  "entry": "main.yv",
  "dependencies": {
    "jwt": "github:santhyago/yvira-jwt",
    "math_extra": "github:usuario/math-pack@v1.2",
    "util": "https://raw.githubusercontent.com/usuario/libs/main/util.yv"
  }
}
```

---

## 2. Comandos da CLI

### A. Restaurar Dependências do Projeto
Para baixar e sincronizar todas as dependências declaradas no `yvira.json`:

```powershell
yvira install
# ou forma abreviada:
yvira i
```

### B. Adicionar uma Nova Dependência Rapidamente
Você pode instalar um pacote diretamente pelo terminal. O Yvira baixa o código e já atualiza o `yvira.json` automaticamente:

```powershell
# Instalar a partir de um repositório GitHub:
yvira install github:santhyago/yvira-jwt

# Instalar com alias customizado:
yvira install meu_jwt=github:santhyago/yvira-jwt

# Instalar módulo utilitário direto de uma URL:
yvira install https://raw.githubusercontent.com/usuario/libs/main/strings.yv
```

---

## 3. Como Utilizar no Código Yvira

Uma vez instalado o pacote, a resolução de `import` do Yvira detecta automaticamente a biblioteca:

```yvira
# 1. Importa pacote instalado localmente via alias do yvira.json:
import "jwt" as jwt;

let token = jwt.gerar_token("usuario_123", "meu_segredo");
print("Token:", token);

# 2. Importa diretamente por URL remota (estilo Deno/Go):
import "https://raw.githubusercontent.com/usuario/libs/main/strings.yv" as strLib;
print(strLib.slugify("Yvira Lang"));
```

---

## 4. Ordem de Resolução dos Módulos (Pipeline de Import)

Ao encontrar `import "nome"`, o runtime do Yvira segue rigorosamente esta prioridade:

1. **Módulo Nativo Embutido**: (`fs`, `http`).
2. **URL Remota**: Se iniciar com `http://` ou `https://`, baixa automaticamente para o cache global e importa.
3. **Arquivo Local no Diretório Atual**: (`./nome.yv`, `./nome.json`, `./nome.env`).
4. **Pacote Local do Projeto**: (`.yvira/packages/nome/index.yv` ou `main.yv`).
5. **Cache Global**: (`~/.yvira/cache/nome/...`).
