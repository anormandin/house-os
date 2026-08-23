#!/usr/bin/env bash
# Backup House OS : dump PostgreSQL + archive des fichiers (manuels/photos).
# Usage : ./scripts/backup.sh [dossier-destination]   (défaut : ./backups)
#
# À planifier : launchd/cron quotidien sur la machine qui héberge docker compose.
# Les backups restent locaux — pensez à en copier hors de la machine de temps en temps.
set -euo pipefail

REPERTOIRE="$(cd "$(dirname "$0")/.." && pwd)"
DESTINATION="${1:-$REPERTOIRE/backups}"
HORODATAGE="$(date +%Y%m%d-%H%M%S)"
RETENTION_JOURS=30

mkdir -p "$DESTINATION"

# 1. Dump de la base (format custom : restaurable sélectivement avec pg_restore)
docker exec houseos-postgres pg_dump -U houseos -d houseos -Fc \
  > "$DESTINATION/houseos-$HORODATAGE.dump"

# 2. Archive du volume de fichiers (manuels et photos des équipements)
docker run --rm \
  -v house-os_fichiers:/fichiers:ro \
  -v "$DESTINATION":/backup \
  alpine tar czf "/backup/fichiers-$HORODATAGE.tar.gz" -C /fichiers .

# 3. Rétention : purge des backups de plus de RETENTION_JOURS jours
find "$DESTINATION" -name 'houseos-*.dump' -mtime +"$RETENTION_JOURS" -delete
find "$DESTINATION" -name 'fichiers-*.tar.gz' -mtime +"$RETENTION_JOURS" -delete

echo "Backup terminé : $DESTINATION/houseos-$HORODATAGE.dump"
echo "                 $DESTINATION/fichiers-$HORODATAGE.tar.gz"

# Restauration (aide-mémoire) :
#   docker exec -i houseos-postgres pg_restore -U houseos -d houseos --clean --if-exists < houseos-<date>.dump
#   docker run --rm -v house-os_fichiers:/fichiers -v "$PWD":/backup alpine tar xzf /backup/fichiers-<date>.tar.gz -C /fichiers
