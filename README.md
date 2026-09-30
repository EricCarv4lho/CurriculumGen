
# Curriculum Generator

Gerador de currículos inteligente com suporte a múltiplos templates, tradução automática, sugestões por IA e gerenciamento de currículos salvos. Desenvolvido com **ASP.NET Core 8** no backend e **Vite + TypeScript** no frontend.

## Funcionalidades

- **Gerar Currículo PDF**: 4 templates visuais (Clássico, Moderno, Executivo, Criativo) com seções customizáveis
- **Tradução por IA**: Envie um PDF de currículo e receba a tradução para 6 idiomas via Groq (Llama 3.3 70B)
- **Sugestões por IA**: Melhore objetivo, experiência e habilidades com inteligência artificial
- **Carta de Apresentação**: Gere cartas de apresentação personalizadas a partir do currículo e descrição da vaga
- **Gerenciamento de Currículos**: CRUD completo de currículos salvos com upload/download de PDF
- **Autenticação JWT**: Sistema de cadastro/login com ASP.NET Core Identity
- **Frontend SPA**: Interface multi-etapas com validação, temas claro/escuro e upload drag-and-drop

## Tecnologias Utilizadas

### Backend
- **ASP.NET Core 8** — API RESTful
- **Entity Framework Core 8** — ORM com PostgreSQL
- **ASP.NET Core Identity** — Autenticação e gerenciamento de usuários
- **JWT Bearer** — Autenticação por tokens
- **iText** — Geração e manipulação de PDFs
- **FluentValidation 12** — Validação de requisições
- **Swashbuckle (Swagger)** — Documentação interativa da API
- **Groq AI (Llama 3.3 70B)** — Tradução, sugestões e geração de carta de apresentação
- **API Versioning** — Versionamento de API por segmento de URL

### Frontend
- **Vite 6** — Build tool e dev server
- **TypeScript 5.7** — Tipagem estática
- **HTML5 + CSS3** — Interface responsiva com temas claro/escuro

### Infraestrutura
- **PostgreSQL** — Banco de dados principal

## Endpoints

### Autenticação (`/api/auth`)

| Método | Rota | Autenticação | Descrição |
|--------|------|-------------|-----------|
| POST | `/api/auth/register` | ❌ | Cadastrar novo usuário |
| POST | `/api/auth/login` | ❌ | Login (retorna JWT, usuário e plano) |
| GET | `/api/auth/me` | ✅ | Dados do usuário autenticado |

### Currículo (`/api/Curriculum`)

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/Curriculum/generate` | Gerar PDF do currículo |
| POST | `/api/Curriculum/translate` | Traduzir PDF de currículo |
| POST | `/api/Curriculum/suggest` | Melhorar texto com IA |
| POST | `/api/Curriculum/cover-letter` | Gerar carta de apresentação em PDF |
| POST | `/api/Curriculum/cover-letter-text` | Gerar texto da carta de apresentação |

### Currículos Salvos (`/api/resumes`)

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/resumes` | Listar currículos do usuário |
| GET | `/api/resumes/{id}` | Obter currículo por ID |
| POST | `/api/resumes` | Criar currículo |
| PUT | `/api/resumes/{id}` | Atualizar currículo |
| DELETE | `/api/resumes/{id}` | Excluir currículo |
| POST | `/api/resumes/{id}/pdf` | Fazer upload de PDF |
| GET | `/api/resumes/{id}/pdf` | Baixar PDF salvo |

## Como Executar

1. Clone o repositório:
   ```bash
   git clone https://github.com/EricCarv4lho/CurriculumGenerator.git
   ```

2. Configure os segredos locais com `dotnet user-secrets` (dentro da pasta `CurriculumGenerator`):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=docgendb;Username=postgres;Password=suasenha;"
   dotnet user-secrets set "Jwt:Key" "sua-chave-jwt-secreta"
   dotnet user-secrets set "Groq:ApiKey" "sua-chave-da-groq"
   ```
   Em produção, use variáveis de ambiente com o mesmo nome (ex.: `Groq__ApiKey`).

3. Restaure os pacotes e aplique as migrations:
   ```bash
   dotnet restore
   dotnet ef database update
   ```

4. Execute a aplicação:
   ```bash
   dotnet run
   ```

5. Acesse o Swagger em: `http://localhost:5277/swagger`

6. Para o frontend, entre na pasta `FrontEnd` e execute:
   ```bash
   npm install
   npm run dev
   ```
