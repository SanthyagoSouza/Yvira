# 16 - Importação de Arquivos de Configuração (.env e .json)

A linguagem **Yvira** agora suporta nativamente a importação de arquivos de configuração em formatos amplamente utilizados: `.env` e `.json`.

---

## 1. Importação de Arquivos `.env`

Os arquivos `.env` são ideais para segredos, portas, hosts e credenciais da aplicação.

### Exemplo de arquivo `.env`:
```ini
# Configurações do Servidor
HOST="127.0.0.1"
PORT=8080
DEBUG=true
DB_NAME=meu_banco_dados
API_KEY="segredo_super_seguro"
```

### Importando com Namespace (`as env`):
```yvira
import ".env" as env

print("Servidor rodando em:", env.HOST)
print("Porta:", env.PORT)
print("Modo debug:", env.DEBUG)
print("Chave API:", env.API_KEY)
```

### Importando diretamente no escopo global:
```yvira
import ".env"

if (DEBUG) {
    print("Modo de depuração ativo na porta:", PORT)
}
```

> **Conversão Automática de Tipos**: Números inteiros/flutuantes e booleanos (`true` / `false`) são convertidos automaticamente para seus respectivos tipos nativos do Yvira.

---

## 2. Importação de Arquivos `.json`

Arquivos `.json` com estruturas aninhadas, listas e mapas são convertidos diretamente para os tipos nativos de Yvira (`YviraMap` e `YviraList`).

### Exemplo de `config.json`:
```json
{
  "nome": "Sistema Yvira",
  "versao": "2.0.0",
  "config": {
    "ativo": true,
    "tentativas": 3
  },
  "tags": ["ia", "compilador", "matrizes"]
}
```

### Importando e manipulando:
```yvira
import "config.json" as cfg

print("Nome do App:", cfg.nome)
print("Tentativas configuradas:", cfg.config.tentativas)
print("Primeira tag:", cfg.tags[0])
```