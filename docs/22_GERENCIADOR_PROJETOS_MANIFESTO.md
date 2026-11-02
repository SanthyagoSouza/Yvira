# 22 - Gerenciador de Projetos e Manifesto (`yvira init` & `yvira.json`)

A linguagem **Yvira** agora possui um sistema de projetos padronizado e profissional, permitindo inicializar novas aplicações estruturadas com um único comando.

---

## 1. O Arquivo de Manifesto `yvira.json`

O arquivo `yvira.json` define a identidade e as configurações do projeto:

```json
{
  "name": "meu_projeto",
  "version": "1.0.0",
  "description": "Projeto desenvolvido na linguagem Yvira",
  "entry": "main.yv",
  "author": "Desenvolvedor Yvira"
}
```

---

## 2. Comandos Disponíveis no CLI

### 1. Criar um novo projeto (`yvira init`)
Cria a pasta do projeto com toda a estrutura inicial:
```bash
yvira init meu_sistema
```

**Estrutura gerada automaticamente:**
```
meu_sistema/
├── yvira.json   <- Manifesto de configuração do projeto
├── main.yv      <- Ponto de entrada padrão
├── .env         <- Variáveis de ambiente configuradas
└── .gitignore   <- Arquivos temporários e logs ignorados
```

---

### 2. Executar o projeto atual (`yvira run` sem argumentos)
Se você estiver dentro da pasta de um projeto que possui `yvira.json`, basta digitar:
```bash
yvira run
```
O Yvira lê automaticamente o arquivo indicado na propriedade `"entry"` e inicia a execução!

---

### 3. Exibir informações do ambiente e do projeto (`yvira info`)
Mostra a versão da linguagem, tempo de execução e os metadados do projeto ativo:
```bash
yvira info
```

**Saída de exemplo:**
```
Informacoes do Ambiente Yvira:
  Versao do Yvira : 2.1.0
  Runtime .NET    : 10.0.11
  Diretorio Atual : C:\Projetos\meu_sistema

Projeto Ativo (yvira.json):
  Nome        : meu_sistema
  Versao      : 1.0.0
  Descricao   : Projeto meu_sistema em Yvira
  Entrypoint  : main.yv
  Autor       : Yvira Developer
```