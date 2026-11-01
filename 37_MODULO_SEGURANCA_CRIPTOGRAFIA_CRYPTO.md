# 37 - Módulo Nativo de Criptografia e Segurança Defensiva (`crypto`)

A partir da versão 2.2, a linguagem **Yvira** passa a contar com o módulo nativo **`crypto`**, fornecendo algoritmos criptográficos modernos, gerador de chaves seguras (CSPRNG), autenticação de mensagens (HMAC), hash de senhas resistente a ataques de força bruta (PBKDF2) e cifra simétrica de nível militar (AES-256).

---

## 1. Importando o Módulo

O módulo é integrado ao ecossistema padrão:
```yvira
import "crypto" as crypto
```

---

## 2. Hashes Criptográficos e Assinaturas Digitais

### Hashing com SHA-256 e SHA-512
```yvira
let hash256 = crypto.sha256("admin@yvira.com.br")
let hash512 = crypto.sha512("admin@yvira.com.br")

print("SHA-256:", hash256)
```

### Autenticação de Mensagens (HMAC-SHA256)
Previne adulteração de requisições e payloads em APIs e Webhooks:
```yvira
let segredo = "CHAVE_SECRETA_API"
let assinatura = crypto.hmac(segredo, "pedido=101&total=450.00")
```

---

## 3. Geração de Entropia e Identificadores Seguros

### CSPRNG (Cryptographically Secure Pseudo-Random Number Generator)
Gera sequências de bytes de alta entropia para tokens de sessão e chaves de API:
```yvira
let tokenSessao = crypto.randomBytes(32) # 256 bits aleatórios
let novoId = crypto.uuid()                # UUID v4 padrão (RFC 4122)
```

---

## 4. Gestão Segura de Senhas (Anti-Brute Force & Timing Attack)

O Yvira implementa **PBKDF2 com 100.000 iterações**, Salt exclusivo de 16 bytes e validação em **tempo constante** (`FixedTimeEquals`), neutralizando Timing Attacks:

```yvira
# 1. Gerando o hash com Salt para salvar no banco (@db)
let hashArmazenar = crypto.hashPassword("MinhaSenhaForte!2026")
# Formato gerado: $pbkdf2$100000$saltHex$hashHex

# 2. Verificando o login do usuário
let valida = crypto.verifyPassword("MinhaSenhaForte!2026", hashArmazenar)
if (valida) {
    print("Acesso autorizado!")
} else {
    print("Credenciais incorretas.")
}
```

---

## 5. Criptografia Ponta a Ponta AES-256

Permite proteger dados sensíveis em repouso (banco de dados) ou em trânsito:
```yvira
let segredoMestre = "Chave-Corporativa-Mestre-Segura-Yvira"
let dados = "Cartao=4532-1198-4421-9988;CVV=882"

# Cifragem com derivação PBKDF2 + AES-256 + IV aleatório
let envelope = crypto.encrypt(dados, segredoMestre)

# Decifragem
let original = crypto.decrypt(envelope, segredoMestre)
print(original)
```

---

## 6. Blindagens Defensivas do Runtime Yvira

Além do módulo `crypto`, o núcleo da linguagem conta com proteções ativas:
1. **Path Traversal Protection (`fs`)**: `FileSystemModule.ResolvePath` valida caminhos canônicos e impede fuga de diretório base (`../../`).
2. **Anti-DoS HTTP (`HttpServerInstance`)**:
   - Bloqueio imediato de payloads maiores que 10MB (`HTTP 413 Payload Too Large`).
   - Cabeçalhos de segurança defensivos automáticos:
     - `X-Content-Type-Options: nosniff`
     - `X-Frame-Options: SAMEORIGIN`
     - `X-XSS-Protection: 1; mode=block`
3. **Escudo Anti-Flood em WebSockets (`@realtime`)**:
   - Rate limiting automático por token bucket fechando conexões que excedam pacotes por segundo configurados.
