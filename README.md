# Lingua

Plataforma de convivência para uma escola de inglês: os alunos publicam em murais, conversam
por mensagem direta, acompanham as aulas gravadas e entregam os trabalhos — tudo em um espaço
fechado, onde a professora vê e modera o que acontece.

O projeto nasceu de uma API de blog e foi reescrito em cima daquela base (autenticação JWT,
Entity Framework com mapeamento explícito, envelope de resposta padronizado).

## O problema

Uma professora de inglês queria um lugar onde os alunos usassem o idioma entre eles fora da
aula. Grupo de mensagens comum não servia: ela não conseguia separar turmas, acompanhar quem
estava participando nem moderar o que era publicado.

## O que a plataforma faz

**Murais**
- Mural geral, visível para todos os alunos da escola
- Mural por turma, restrito a quem está matriculado
- Assuntos (Grammar, Free Talk, Culture...), tags, comentários e curtidas
- Professora esconde e republica qualquer post ou comentário

**Mensagens diretas**
- Conversa privada entre duas pessoas, entregue em tempo real
- Um aluno só consegue abrir conversa com colegas de turma e com a professora
- Qualquer aluno sinaliza uma mensagem para revisão da professora

**Área de estudo**
- Aulas agendadas com link do encontro
- Gravação publicada depois da aula
- Presença registrada pelo aluno, que vira histórico no perfil
- Materiais (vídeo, artigo, podcast, exercício) por turma ou para a escola inteira

**Trabalhos**
- Link do enunciado e prazo, por turma
- Aluno entrega o link do trabalho; reenvio atualiza a mesma entrega
- Professora corrige com nota e feedback

**Perfis e ranking**
- Perfil com nível (A1 a C2), bio, turmas e estatísticas
- Ranking de interação por turma e por período

A pontuação do ranking premia o que faz a turma conversar:

| Ação | Pontos |
| --- | --- |
| Publicar um post | 10 |
| Comentar | 4 |
| Entregar um trabalho | 20 |
| Participar de uma aula | 15 |
| Receber uma curtida | 2 |
| Curtir / mandar mensagem | 1 |

## Controle de acesso

Não existe cadastro aberto: a professora convida cada aluno e a senha inicial vai por e-mail.
As regras de visibilidade ficam todas em `Services/AccessService.cs` — nenhum controller ou
página decide sozinho o que alguém pode ver.

| Perfil | Pode |
| --- | --- |
| Aluno | Ler e publicar nos murais que alcança, conversar com quem divide turma, entregar trabalhos |
| Professor | Tudo do aluno, mais criar turmas, matricular, agendar aulas, publicar materiais e trabalhos, corrigir e moderar |
| Admin | Tudo do professor, mais gerenciar professores |

## Tecnologias

- .NET 10 / ASP.NET Core
- Blazor Server (interface) e API REST com JWT (clientes externos)
- Entity Framework Core — SQLite em desenvolvimento, PostgreSQL em produção
- SignalR para as mensagens diretas
- Swagger em desenvolvimento

## Rodando

```bash
dotnet run
```

Sobe em `https://localhost:5001`, cria o banco SQLite (`lingua.db`) e semeia papéis, assuntos
e a conta de professor. A senha inicial dessa conta aparece no log do primeiro boot:

```
warn: Seeder[0] Conta de professor criada: teacher@lingua.local / senha inicial: ...
```

Para escolher o e-mail, o nome e a senha dessa conta:

```bash
dotnet user-secrets set "Seed:TeacherName" "Nome da professora"
dotnet user-secrets set "Seed:TeacherEmail" "professora@exemplo.com"
dotnet user-secrets set "Seed:TeacherPassword" "uma_senha_forte"
```

### Configuração

```bash
dotnet user-secrets set "JwtKey" "sua_chave_com_pelo_menos_32_caracteres"
dotnet user-secrets set "SmtpConfiguration:Host" "smtp.exemplo.com"
dotnet user-secrets set "SmtpConfiguration:UserName" "usuario"
dotnet user-secrets set "SmtpConfiguration:Password" "senha"
```

Sem SMTP configurado a plataforma continua funcionando: o convite não é enviado e a senha
inicial do aluno aparece na tela de gerenciar e no log.

### Produção com PostgreSQL

```json
{
  "Database": { "Provider": "postgres" },
  "ConnectionStrings": { "DefaultConnection": "Host=...;Database=lingua;Username=...;Password=..." },
  "PublicUrl": "https://seu-dominio"
}
```

Migration de EF é específica de banco, então existem dois conjuntos, um por provider, cada um
com seu contexto:

```bash
dotnet ef migrations add NomeDaMigration --context SqliteDataContext   --output-dir Migrations/Sqlite   --namespace Lingua.Migrations.Sqlite
dotnet ef migrations add NomeDaMigration --context PostgresDataContext --output-dir Migrations/Postgres --namespace Lingua.Migrations.Postgres
```

Toda mudança de modelo precisa dos dois comandos. A aplicação aplica as migrations do provider
configurado na inicialização.

## Organização

```
Components/     Interface em Blazor (Pages, Layout, Shared)
Controllers/    API REST, uma controller por área
Services/       Regras de negócio: acesso, posts, conversas, ranking, perfis
Models/         Entidades
Data/           Contextos por provider, mapeamentos e seed
ViewModels/     Entrada e saída da API e dos formulários
Hubs/           Hub SignalR das mensagens
Migrations/     Um conjunto por provider
```

Duas decisões que valem explicação:

- `Services/ScopeRunner.cs` — no Blazor Server o escopo de DI dura o circuito inteiro. As
  páginas pedem os serviços por ele, e cada operação ganha um `DbContext` novo e descartável
  em vez de um que viveria horas acumulando entidades.
- `Services/ChatNotifier.cs` — a página de mensagens roda no próprio servidor, então escuta um
  notificador em processo em vez de abrir um cliente SignalR contra o hub local. O hub segue
  disponível para clientes externos da API.

## API

Autenticação por `Authorization: Bearer <token>` de `POST /v1/accounts/login`.
Swagger em `/swagger` no ambiente de desenvolvimento.

| Área | Endpoints |
| --- | --- |
| Contas | `POST /v1/accounts`, `POST /v1/accounts/login`, `PUT /v1/accounts/me`, `PUT /v1/accounts/me/password`, `POST /v1/accounts/me/image` |
| Turmas | `GET|POST /v1/classrooms`, `GET|PUT|DELETE /v1/classrooms/{id}`, `POST|DELETE /v1/classrooms/{id}/students` |
| Posts | `GET|POST /v1/posts`, `GET|PUT|DELETE /v1/posts/{id}`, `POST /v1/posts/{id}/reactions`, `POST /v1/posts/{id}/comments`, `PUT /v1/posts/{id}/moderation` |
| Assuntos | `GET|POST /v1/topics`, `DELETE /v1/topics/{id}` |
| Mensagens | `GET|POST /v1/conversations`, `GET /v1/conversations/contacts`, `GET|POST /v1/conversations/{id}/messages`, `POST /v1/messages/{id}/flag` |
| Estudo | `GET|POST /v1/lessons`, `PUT|DELETE /v1/lessons/{id}`, `POST /v1/lessons/{id}/attendance`, `GET|POST /v1/resources` |
| Trabalhos | `GET|POST /v1/assignments`, `PUT|DELETE /v1/assignments/{id}`, `POST /v1/assignments/{id}/submissions`, `GET /v1/assignments/{id}/submissions`, `PUT /v1/submissions/{id}/grade` |
| Perfis e ranking | `GET /v1/profiles/{slug}`, `GET /v1/ranking` |

## Licença

MIT.
