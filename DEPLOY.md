# Deploy grátis: Vercel (front) + Render (API) + Supabase/Neon (banco)

| Peça | Onde | Custo |
|---|---|---|
| Front (Vite) | Vercel — plano Hobby | R$0 |
| API (.NET 8, Docker) | Render — plano Free | R$0 |
| PostgreSQL | Supabase ou Neon — plano Free | R$0 |

Limitações do grátis: o Render desliga o serviço após ~15 min sem tráfego (a próxima
visita demora ~30–60 s pra acordar) e o Supabase pausa o projeto após ~1 semana
inativo (restaure com 1 clique no dashboard dele; o Neon acorda sozinho na conexão).
A seção 4 mostra como minimizar isso.

O fluxo do tráfego: `https://seu-app.vercel.app` → Vercel serve o front e faz rewrite
de `/api/*` para a API no Render (sem CORS, sem mudança de código). As migrations do
EF Core são aplicadas automaticamente no startup da API.

## 0. Repositório no GitHub

Tudo aqui parte do código num repositório do GitHub. Se ainda não subiu:

```bash
git remote add origin https://github.com/SEU-USUARIO/CurriculumGen.git
git push -u origin main
```

## 1. Banco (escolha um dos dois)

**Supabase** (o que você mencionou):
1. Crie uma conta em [supabase.com](https://supabase.com) → **New project** (guarde a senha do banco).
2. No projeto: **Connect → Connection string → URI**, aba **Session pooler** (porta `5432`).
   Copie a string trocando `[YOUR-PASSWORD]` pela senha que criou.

**Neon** (alternativa que acorda sozinha):
1. Crie conta em [neon.com](https://neon.com) → projeto.
2. Copie a **connection string pooled** (a que tem `-pooler` no host).

> Use a string **session/pooled na porta 5432**, não a "transaction pooler" (porta 6543) —
> esta última não funciona bem com EF Core/Identity. Guarde a string pro passo 2.

## 2. API no Render

1. Em [dashboard.render.com](https://dashboard.render.com): **New → Blueprint**, selecione
   o repositório (ele lê o `render.yaml` da raiz e já configura plano Free + health check).
2. O Render pedirá os valores marcados como `sync: false`:

   | Variável | Valor |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | string do passo 1 |
   | `Jwt__Key` | gere com `openssl rand -base64 48` (Git Bash tem openssl) |
   | `Groq__ApiKey` | sua chave do console do Groq |
   | `MercadoPago__AccessToken` / `MercadoPago__BackendUrl` | só se for usar pagamentos; BackendUrl = `https://curriculumgen-api.onrender.com` |

3. **Apply** e aguarde o build (Dockerfile) + deploy. No primeiro boot a API cria o
   schema via migrations.
4. Anote a URL pública, ex.: `https://curriculumgen-api.onrender.com`. Teste no navegador:
   `https://curriculumgen-api.onrender.com/health` deve responder `ok` (primeira carga
   pode demorar ~1 min acordando).

Se preferir criar manualmente em vez do Blueprint: **New → Web Service** → runtime
**Docker** → repositório → plano Free, e cadastre as mesmas variáveis em **Environment**.

## 3. Front na Vercel

1. Em [vercel.com](https://vercel.com): **Add New → Project** → importe o repositório.
2. Em **Root Directory**, selecione `FrontEnd` (o framework Vite é detectado sozinho).
3. **Deploy**.
4. Edite `FrontEnd/vercel.json` trocando `CURRICULUMGEN-API.onrender.com` pela URL real
   do Render e faça commit — a Vercel redeploya sozinha.

Pronto: `https://seu-app.vercel.app` já fala com a API. Teste criar conta → entrar →
gerar um currículo → baixar PDF.

## 4. (Opcional) manter acordado

Crie um ping grátis em [cron-job.org](https://cron-job.org): `GET https://curriculumgen-api.onrender.com/health`
a cada 10 minutos. Isso evita o cold start do Render. Não evita a pausa do Supabase
(o `/health` não toca o banco) — pra isso o Neon é mais prático, pois acorda sozinho.

## Se algo der errado

- **Primeiro acesso demora / 502**: serviço acordando (plano Free) — recarregue depois de ~1 min.
- **Erro de conexão com banco nos logs** (Render → **Events/Logs**): confira a string
  (Supabase: session pooler porta 5432; senha com caracteres especiais precisa URL-encode).
- **`/api` retorna 404 na Vercel**: o `vercel.json` ainda está com a URL placeholder — passo 3.4.
