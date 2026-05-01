# Vendly Server — Deployment & Infrastructure

> Bu hujjat: production server'da vendly-server qanday ishga tushirilgan, qanday yangilanadi, qanday muammolarini hal qilish kerak. Yangi dasturchi (sen yoki men ketgandan keyingi odam) buni o'qib hammasini tushunishi va o'zgartira olishi kerak.

---

## 1. Kirish — Arxitektura

### 1.1 Bitta serverda nima ishlaydi

Hammasi **bitta Ubuntu serverda** ishlaydi. Production va Staging — yonma-yon, ammo izolyatsiyalangan.

```
┌────────────────────────────────────────────────────────────────────────┐
│                       Ubuntu Server (95.182.118.55)                     │
│                                                                          │
│   ┌─────────────────────────────────────────────────────────────┐      │
│   │ HOST                                                         │      │
│   │  • Postgres 16+   listen 0.0.0.0:5432                       │      │
│   │  • Nginx          listen 80, 443                            │      │
│   │  • UFW            firewall (allow 22, 80, 443, 150, 151)    │      │
│   └─────────────────────────────────────────────────────────────┘      │
│                                                                          │
│   ┌─────────────────────────────────────────────────────────────┐      │
│   │ DOCKER                                                       │      │
│   │                                                              │      │
│   │  ┌─── network: vendly-net (shared, external) ──────┐       │      │
│   │  │                                                   │       │      │
│   │  │  vendly-minio              :9000, :9001         │       │      │
│   │  │   (MinIO server)            (127.0.0.1 only)     │       │      │
│   │  │                                                   │       │      │
│   │  │  vendly-prod    ◄───┐                            │       │      │
│   │  │  vendly-staging ◄───┴── joined here too         │       │      │
│   │  │                                                   │       │      │
│   │  └───────────────────────────────────────────────────┘       │      │
│   │                                                              │      │
│   │  ┌── network: vendly-prod-net ────────┐                    │      │
│   │  │  vendly-prod         :150 → :8080  │                    │      │
│   │  │  vendly-seq-prod     :8082, :5341  │                    │      │
│   │  └─────────────────────────────────────┘                    │      │
│   │                                                              │      │
│   │  ┌── network: vendly-staging-net ─────┐                    │      │
│   │  │  vendly-staging      :151 → :8080  │                    │      │
│   │  │  vendly-seq-staging  :8083, :5342  │                    │      │
│   │  └─────────────────────────────────────┘                    │      │
│   └─────────────────────────────────────────────────────────────┘      │
└────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Tarmoqlar (Docker networks)

| Network | Maqsad | Ulanganlar |
|---|---|---|
| `vendly-prod-net` | Prod ichki tarmog'i | `vendly-prod` ↔ `vendly-seq-prod` |
| `vendly-staging-net` | Staging ichki tarmog'i | `vendly-staging` ↔ `vendly-seq-staging` |
| `vendly-net` | MinIO va apps uchun shared | `vendly-minio` ↔ `vendly-prod` ↔ `vendly-staging` |

`vendly-net` — **external** tarmoq (CI tomonidan yaratilgan, alohida turadi). Apps har biri **ikkita tarmoqda**: o'zining (seq bilan) + `vendly-net` (MinIO bilan).

### 1.3 Domenlar va portlar

| Domain | Backend | Internal port |
|---|---|---|
| `api-opto.vestor.uz` | `vendly-prod` | `127.0.0.1:150 → :8080` |
| `stage-opto.vestor.uz` | `vendly-staging` | `127.0.0.1:151 → :8080` |
| `files.vestor.uz` | `vendly-minio` (S3 API) | `127.0.0.1:9000` |
| `files.vestor.uz/console/` | `vendly-minio` (Admin UI) | `127.0.0.1:9001` |

**Hech qachon** MinIO portlarini (`9000`, `9001`) public expose qilma — Nginx ulardan oldida turadi va HTTPS terminate qiladi.

### 1.4 Bucketlar

MinIO'da ikkita bucket:

| Bucket | Kim ishlatadi | Public URL prefix |
|---|---|---|
| `prod-vendly` | `vendly-prod` | `https://files.vestor.uz/prod-vendly/...` |
| `stage-vendly` | `vendly-staging` | `https://files.vestor.uz/stage-vendly/...` |

Ikkalasi ham **anonymous download** policy bilan — har kim URL'ni ochib rasm/file ko'ra oladi (yuklay olmaydi).

---

## 2. CI/CD — Qanday deploy qilinadi

### 2.1 Branch strategiyasi

| Branch | Trigger | Nima bo'ladi |
|---|---|---|
| `staging` | Push | Image quriladi, `vendly-staging` qayta deploy bo'ladi, MinIO tekshiriladi |
| `main` | Push (PR'dan merge orqali) | Image quriladi, `vendly-prod` qayta deploy bo'ladi, MinIO tekshiriladi |
| boshqa | Push | Hech narsa bo'lmaydi |

**Workflow:** dasturchi `staging` branch'da ishlab, test qiladi → keyin PR ochib `main`'ga merge qiladi → prod yangilanadi.

### 2.2 GitHub Actions jobs

Fayl: `.github/workflows/ci-cd.yml`

```
push to staging or main
   │
   ├──> build_and_push       (har doim ishlaydi)
   │      • Docker image quradi
   │      • ghcr.io ga push qiladi
   │      • Tag: <git-sha> + <branch-name>
   │
   ├──> deploy_minio         (har doim ishlaydi, IDEMPOTENT)
   │      • SSH to server
   │      • $HOME/vendly-minio/ papkasini yaratadi
   │      • docker-compose.yml + .env yozadi
   │      • docker network create vendly-net
   │      • docker compose up -d (agar o'zgargan bo'lsa qayta yaratadi)
   │
   └──> deploy_staging       (faqat staging branch'da)
        deploy_prod          (faqat main branch'da)
          • SSH to server
          • $HOME/vendly-{staging,prod}/docker-compose.yml ni yangilaydi
          • Yangi image'ni pull qiladi
          • docker compose down && up -d
```

### 2.3 GitHub secrets (Repository Settings → Secrets)

Bu secret'lar GitHub Actions tomonidan ishlatiladi. **Repo'ga commit qilinmaydi.**

| Secret | Misol | Maqsad |
|---|---|---|
| `SERVER_IP` | `95.182.118.55` | Deploy uchun SSH host |
| `SERVER_USERNAME` | `opto` | SSH user |
| `SSH_PRIVATE_KEY` | `-----BEGIN OPENSSH...` | SSH privat kalit |
| `GH_TOKEN` | `ghp_...` | GHCR'ga login uchun (packages: write) |

Yangi secret qo'shmoqchi bo'lsang: **GitHub repo → Settings → Secrets and variables → Actions → New repository secret**.

### 2.4 Deploy qilish — odatiy oqim

**Yangi feature qilish:**
```bash
git checkout staging
git pull
# kod yoz
git add .
git commit -m "feat: add X"
git push
# CI ishga tushadi → staging deploy bo'ladi
# stage-opto.vestor.uz da test qil
```

**Prod'ga chiqarish:**
```bash
# GitHub'da PR och: staging → main
# Code review
# Merge
# CI avtomatik prod'ga deploy qiladi
```

**Hot fix to'g'ridan-to'g'ri main'ga (faqat zarur bo'lganda):**
```bash
git checkout main
git pull
git checkout -b hotfix/critical
# fix yoz
git push
# PR och, tezda merge qil
```

---

## 3. Server fayl tuzilmasi

SSH bilan kirsang (`opto@95.182.118.55`), bular bor:

```
/home/opto/
├── vendly-prod/
│   └── docker-compose.yml       # CI generate qiladi (deploy_prod)
│
├── vendly-staging/
│   └── docker-compose.yml       # CI generate qiladi (deploy_staging)
│
└── vendly-minio/
    ├── docker-compose.yml       # CI generate qiladi (deploy_minio)
    └── .env                     # MinIO credentials (chmod 600)
```

**Bu fayllarni qo'l bilan tahrirlamang.** CI har deployda qayta yozadi. O'zgartirish kerak bo'lsa — `.github/workflows/ci-cd.yml` ni o'zgartir.

### Volumes (Docker named volumes)

```bash
docker volume ls | grep -E "seq|minio"
```

| Volume | Saqlaydi |
|---|---|
| `vendly-prod_seq-prod-data` | Prod log'lar (Seq) |
| `vendly-staging_seq-staging-data` | Staging log'lar (Seq) |
| `vendly-minio_minio-data` | MinIO bucket fayllari (rasmlar) |

Bu volume'lar **CI deploy'larda saqlanib qoladi** — `docker compose down` faqat container'larni o'chiradi, volume'larni emas. Faqat qo'l bilan `docker volume rm` qilsangina yo'qoladi.

---

## 4. Nginx konfiguratsiyasi

Vhost fayllari: `/etc/nginx/sites-available/`, symlink qilingan: `/etc/nginx/sites-enabled/`.

```bash
ls /etc/nginx/sites-enabled/
# default
# minio_vendly         → files.vestor.uz
# prod_vendly_backend  → api-opto.vestor.uz
# stage_vendly_backend → stage-opto.vestor.uz
```

### 4.1 MinIO vhost (`minio_vendly`)

`files.vestor.uz` — MinIO console + S3 API.

```nginx
server {
    listen 80;
    server_name files.vestor.uz;
    if ($host = files.vestor.uz) { return 301 https://$host$request_uri; }
    return 404;
}

server {
    listen 443 ssl;
    http2 on;
    server_name files.vestor.uz;

    ssl_certificate     /etc/letsencrypt/live/files.vestor.uz/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/files.vestor.uz/privkey.pem;
    include             /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam         /etc/letsencrypt/ssl-dhparams.pem;

    client_max_body_size 100M;
    proxy_request_buffering off;
    proxy_buffering off;
    chunked_transfer_encoding off;
    proxy_connect_timeout 300s;
    proxy_send_timeout    300s;
    proxy_read_timeout    300s;

    # MinIO admin UI
    location /console/ {
        rewrite ^/console/(.*) /$1 break;
        proxy_pass http://127.0.0.1:9001;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade           $http_upgrade;
        proxy_set_header Connection        "upgrade";
    }

    # MinIO S3 API (bucket public reads + app uploads)
    location / {
        proxy_pass http://127.0.0.1:9000;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 4.2 App vhost'lari (prod va stage)

`api-opto.vestor.uz` — port 150'ga proxy qiladi.
`stage-opto.vestor.uz` — port 151'ga proxy qiladi.

### 4.3 Nginx o'zgartirsang

```bash
sudo nano /etc/nginx/sites-available/<filename>
sudo nginx -t                    # syntax tekshir
sudo systemctl reload nginx      # qayta yukla (downtime yo'q)
```

### 4.4 SSL cert yangilash

Let's Encrypt cert'lar 90 kun. Avtomatik yangilanadi:
```bash
sudo systemctl status certbot.timer    # ishlayotganini tekshir
sudo certbot renew --dry-run           # test
```

Agar yangi domain qo'shsang:
```bash
sudo certbot --nginx -d <new-domain>
```

---

## 5. Konfiguratsiya — qayerda nima turadi

### 5.1 .NET app config

| Fayl | Qachon ishlatiladi | Saqlaydi |
|---|---|---|
| `appsettings.json` | Har doim (asos) | Default qiymatlar |
| `appsettings.Development.json` | Lokal `dotnet run` | Dev DB connection |
| `appsettings.Staging.json` | `ASPNETCORE_ENVIRONMENT=Staging` | Stage DB, Stage MinIO |
| `appsettings.Production.json` | `ASPNETCORE_ENVIRONMENT=Production` | Prod DB, Prod MinIO |

### 5.2 Hozirgi prod/stage config (eslatma)

**`appsettings.Production.json`:**
```jsonc
{
  "ConnectionStrings": {
    "DefaultConnectionString": "Host=host.docker.internal;Port=5432;Database=prod_vendly_db;..."
  },
  "Minio": {
    "Endpoint": "minio:9000",
    "AccessKey": "vendlyadmin",
    "SecretKey": "rI0eSKvglRFjLz0TxhrlBB5I0boruQnf",
    "BucketName": "prod-vendly",
    "PublicBaseUrl": "https://files.vestor.uz",
    "UseSsl": false
  }
}
```

**`appsettings.Staging.json`:** Xuddi shunday, lekin `BucketName: "stage-vendly"` va `Database=stage_vendly_db`.

### 5.3 Nima uchun `host.docker.internal` ?

App container ichidan host Postgres'ga ulanish uchun. Container `host-gateway` orqali host'ga yetadi. UFW'da Docker bridge subnet (172.16.0.0/12, 10.0.0.0/8) 5432 portga ruxsat berilgan.

### 5.4 Nima uchun `Endpoint=minio:9000` ?

Apps `vendly-net` tarmog'ida bo'lgani uchun — Docker DNS'ida `minio` hostname `vendly-minio` container'iga resolve bo'ladi. HTTPS'ga ehtiyoj yo'q (ichki tarmoq).

### 5.5 Nima uchun `PublicBaseUrl=https://files.vestor.uz` ?

App'ning **client'larga qaytaradigan** URL. Brauzer/mobile MinIO'ga `files.vestor.uz` orqali yetadi (Nginx → port 9000).

---

## 6. Odatiy operatsiyalar (How to ...)

### 6.1 Stage'da yangi feature deploy qilish

```bash
git checkout staging
git pull
# kod yoz, test qil
git push
# 2-3 daqiqa kut, Github Actions tab'ida ko'rinishini tekshir
```

Tekshirish: `https://stage-opto.vestor.uz/swagger`.

### 6.2 Prod'ga release qilish

GitHub'da PR och: `staging → main`. Code review, keyin merge. CI prod'ga avtomatik deploy qiladi.

### 6.3 MinIO password'ni rotation qilish

1. Yangi password yarat:
   ```bash
   openssl rand -base64 32 | tr -dc 'A-Za-z0-9' | head -c 32
   ```
2. **Uchta joyda** o'zgartir (ikkalasi ham bir xil bo'lishi shart):
   - `appsettings.Production.json` → `Minio.SecretKey`
   - `appsettings.Staging.json` → `Minio.SecretKey`
   - `.github/workflows/ci-cd.yml` → `MINIO_ROOT_PASSWORD` qiymati (deploy_minio job ichida)
3. Commit, push to `main`. CI prod ham, MinIO'ni ham qayta deploy qiladi.

> ⚠️ **Diqqat:** MinIO container password'ni faqat birinchi ishga tushganda o'rnatadi. Eski volume bilan yangi password ishlamaydi. Password'ni o'zgartirgandan keyin server'da:
> ```bash
> docker stop vendly-minio
> docker volume rm vendly-minio_minio-data    # eski fayllar yo'qoladi!
> # CI keyingi pushda qayta yaratadi
> ```
> **Yoki** MinIO admin UI orqali yangi user yaratish — bu xavfsizroq.

### 6.4 Yangi bucket qo'shish

`.github/workflows/ci-cd.yml` ichida `deploy_minio` job → `createbuckets` ning `entrypoint`:

```yaml
mc mb --ignore-existing local/prod-vendly &&
mc mb --ignore-existing local/stage-vendly &&
mc mb --ignore-existing local/<NEW-BUCKET> &&        # qo'sh
mc anonymous set download local/prod-vendly &&
mc anonymous set download local/stage-vendly &&
mc anonymous set download local/<NEW-BUCKET> &&      # qo'sh
```

Push, CI bucket'ni avtomatik yaratadi.

### 6.5 App ichida MinIO'dan tashqari yangi bucket'ni ishlatish

Hozir `IStorageService` har doim `MinioOptions.BucketName`'ga yozadi. Agar boshqa bucket kerak bo'lsa — `MinioStorageService.UploadAsync(file, folder, ct)` parametriga yangi argument qo'sh yoki yangi service yaratasen.

### 6.6 DB password rotation

1. Postgres'da yangi password o'rnat:
   ```bash
   sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'NEW_PASSWORD';"
   ```
2. `appsettings.Production.json` va `appsettings.Staging.json` → `ConnectionStrings.DefaultConnectionString` o'zgartir.
3. Commit, push.

### 6.7 Nginx vhost qo'shish (yangi subdomain)

1. DNS A record yarat: `<new>.vestor.uz` → server IP.
2. Server'da:
   ```bash
   sudo nano /etc/nginx/sites-available/<filename>
   ```
   ```nginx
   server {
       listen 80;
       server_name <new>.vestor.uz;
       location / { proxy_pass http://127.0.0.1:<port>; }
   }
   ```
3. Symlink + reload:
   ```bash
   sudo ln -s /etc/nginx/sites-available/<filename> /etc/nginx/sites-enabled/
   sudo nginx -t && sudo systemctl reload nginx
   sudo certbot --nginx -d <new>.vestor.uz
   ```

### 6.8 Log'larni ko'rish

**App log'lari (real-time):**
```bash
docker logs vendly-prod    --tail 100 -f
docker logs vendly-staging --tail 100 -f
```

**Seq UI orqali:**
- Prod: `http://<server-ip>:8082` (admin / `Vendly@SeqProd2026!`)
- Stage: `http://<server-ip>:8083` (admin / `Vendly@SeqStage2026!`)

**Nginx access log:**
```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

---

## 7. Boshlangichdan ishga tushirish (yangi server)

Agar serverni yo'qotsang yoki yangi serverga ko'chmoqchi bo'lsang, shu qadamlar:

### 7.1 Server tayyorlash
```bash
# Docker
sudo apt update
sudo apt install -y docker.io docker-compose-plugin

# Nginx + Certbot
sudo apt install -y nginx certbot python3-certbot-nginx

# Postgres
sudo apt install -y postgresql

# UFW
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 150/tcp                                       # prod app
sudo ufw allow 151/tcp                                       # stage app
sudo ufw allow from 172.16.0.0/12 to any port 5432 proto tcp
sudo ufw allow from 10.0.0.0/8    to any port 5432 proto tcp
sudo ufw enable
```

### 7.2 Postgres sozlash
```bash
# /etc/postgresql/16/main/postgresql.conf — listen_addresses = '*'
# /etc/postgresql/16/main/pg_hba.conf — Docker bridge'larga ruxsat:
#   host all all 172.16.0.0/12 md5
#   host all all 10.0.0.0/8    md5

sudo systemctl restart postgresql

sudo -u postgres psql <<EOF
CREATE DATABASE prod_vendly_db;
CREATE DATABASE stage_vendly_db;
ALTER USER postgres PASSWORD 'K@3wF!8zQ2#eRt';
EOF
```

### 7.3 DNS
A records:
- `api-opto.vestor.uz` → server IP
- `stage-opto.vestor.uz` → server IP
- `files.vestor.uz` → server IP

### 7.4 Nginx
Vhostlarni yarat (`prod_vendly_backend`, `stage_vendly_backend`, `minio_vendly`), keyin:
```bash
sudo certbot --nginx -d api-opto.vestor.uz
sudo certbot --nginx -d stage-opto.vestor.uz
sudo certbot --nginx -d files.vestor.uz
```

`minio_vendly` vhost'ini section 4.1'dagi to'liq kontent bilan almashtir.

### 7.5 GitHub secrets sozla
Section 2.3'dagi 4 ta secret'ni qo'sh.

### 7.6 SSH key
Server'da `~/.ssh/authorized_keys`'ga GitHub Actions uchun privat kalit bog'lanadigan public kalit qo'sh.

### 7.7 Push qil

`staging` yoki `main` branch'ga push qilsang, CI hammasini avtomatik o'rnatadi:
- `vendly-net` tarmog'i yaratiladi
- MinIO container chiqadi
- Bucketlar yaratiladi
- Apps deploy bo'ladi

---

## 8. Troubleshooting

### 8.1 502 Bad Gateway (Nginx'dan)

Nginx upstream'ga yeta olmayapti. Tekshir:
```bash
docker ps | grep <service>          # container ishlayapti?
curl -I http://127.0.0.1:<port>/    # local'dan javob beryaptimi?
```

Agar container "Up" bo'lsa lekin curl javob bermasa — app crash qilgan bo'lishi mumkin:
```bash
docker logs <container> --tail 100
```

### 8.2 App "Connection refused" Postgres'ga

UFW rules yo'q yoki Postgres tinglamayapti.
```bash
sudo ss -tlnp | grep 5432           # Postgres listen qilyaptimi?
sudo ufw status | grep 5432         # UFW rules borligini tekshir
```

Yo'q bo'lsa:
```bash
sudo ufw allow from 172.16.0.0/12 to any port 5432 proto tcp
sudo ufw allow from 10.0.0.0/8    to any port 5432 proto tcp
sudo ufw reload
```

### 8.3 Seq container restart loop

`SEQ_FIRSTRUN_ADMINPASSWORD` faqat bo'sh volume'ga ishlaydi. Eski stuck volume bo'lsa:
```bash
docker stop vendly-seq-staging
docker rm   vendly-seq-staging
docker volume rm vendly-staging_seq-staging-data
cd ~/vendly-staging
docker compose up -d seq-staging
```

(Prod uchun ham xuddi shu, faqat `staging` → `prod`.)

### 8.4 MinIO console 404

Nginx vhost `minio_vendly` ichida `location /console/` bloki yo'q. Section 4.1'dagi to'liq config bilan almashtir.

### 8.5 Image yuklamayapti, "InvalidUrl" yoki "UploadFailed"

App MinIO'ga ulana olmayapti.
```bash
docker exec vendly-staging sh -c "timeout 3 cat < /dev/tcp/minio/9000 && echo OK || echo FAIL"
```

`FAIL` bo'lsa — app `vendly-net` tarmog'ida emas:
```bash
docker network inspect vendly-net | grep -A2 vendly-staging
```

Agar yo'q bo'lsa — `ci-cd.yml`'da `deploy_staging` ichida `vendly-net` qo'shilganligini tekshir.

### 8.6 CI deploy fail bo'ladi "container name already in use"

Eski container'lar yo'qotilmagan. Qo'l bilan tozalash:
```bash
docker stop vendly-staging vendly-seq-staging
docker rm   vendly-staging vendly-seq-staging
# CI'ni qayta tetik
```

CI'da `docker compose down` chaqiriqi bor — odatda bu bo'lmasligi kerak. Lekin start'da bu xato chiqsa, demak docker holati buzulgan.

### 8.7 SSL cert renew fail

```bash
sudo certbot renew --dry-run
sudo systemctl status certbot.timer
```

DNS o'zgargan bo'lsa, A record'ni tekshir.

---

## 9. Xavfsizlik checklist

- [x] UFW faqat: 22, 80, 443, 150, 151 + Docker bridge → 5432
- [x] MinIO portlari faqat `127.0.0.1` ga bound
- [x] Postgres faqat host loopback + Docker bridge'lardan reachable
- [x] HTTPS hamma public domenlarda (`api-opto`, `stage-opto`, `files`)
- [x] MinIO bucket policy `download` (read-only public), **NOT** `public` (anonymous write blocked)
- [x] `.env` chmod 600
- [x] GitHub secrets'da SSH key, GH_TOKEN
- [ ] **Future:** MinIO uchun root user o'rniga scoped service account yaratish
- [ ] **Future:** Image upload'ga magic-bytes validation qo'shish
- [ ] **Future:** Postgres'ni Docker container'ga ko'chirish (UFW rules'siz)

---

## 10. Asosiy fayllar (Reference)

| Fayl | Maqsad |
|---|---|
| `.github/workflows/ci-cd.yml` | CI/CD — deploy logic |
| `docker-compose.yml` (root) | Lokal dev uchun (CI ishlatmaydi) |
| `dockerfile` | App image build |
| `src/VendlyServer.Api/appsettings.{Production,Staging}.json` | Env-specific config |
| `src/VendlyServer.Application/Services/Storage/` | MinIO integration |
| `src/VendlyServer.Application/Dependencies.cs` | DI registration (`IMinioClient` singleton) |

---

## 11. Loglar va monitoring

| Maqsad | Qayerda |
|---|---|
| App logs (live) | `docker logs vendly-{prod,staging} -f` |
| App logs (centralized) | Seq UI: `:8082` (prod), `:8083` (stage) |
| Nginx access | `/var/log/nginx/access.log` |
| Nginx errors | `/var/log/nginx/error.log` |
| Postgres logs | `/var/log/postgresql/` |
| Docker events | `docker events` |
| MinIO logs | `docker logs vendly-minio` |
| Disk usage | `df -h`, `docker system df` |
| Container stats | `docker stats` |

---

## 12. Aloqa va eslatmalar

- **Repo:** `https://github.com/vendly-team/vendly`
- **Server:** `opto@95.182.118.55` (SSH key bilan)
- **MinIO admin:** `https://files.vestor.uz/console/` — `vendlyadmin` / [secret in ci-cd.yml]
- **Seq prod:** `http://95.182.118.55:8082` — `admin` / `Vendly@SeqProd2026!`
- **Seq stage:** `http://95.182.118.55:8083` — `admin` / `Vendly@SeqStage2026!`

> Yangi dasturchi sifatida ishlay boshlasang: birinchi galda staging'da kichik o'zgarish qilib push qil, CI qanday ishlashini ko'r. Keyin bu hujjatdagi har bir bo'limni ko'zdan kechir.
>
> Savol bo'lsa — bu hujjatni Claude'ga ko'rsat, u kontekstni tushunadi.

---

**Oxirgi yangilanish:** 2026-05-02
**Hujjat versiyasi:** 1.0
