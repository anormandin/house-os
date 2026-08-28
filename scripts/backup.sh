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

# 1. Dump de la base (format custom : restaurable sélectivement avec pg_restore).
#    Écrit dans le conteneur, vérifié par pg_restore --list (un dump tronqué ne se
#    liste pas), puis déplacé en place d'un coup : jamais de fichier partiel
#    d'apparence valide dans $DESTINATION.
TMP_CONTENEUR=/tmp/houseos-backup.dump
docker exec houseos-postgres sh -c \
  "pg_dump -U houseos -d houseos -Fc -f '$TMP_CONTENEUR' \
   && pg_restore --list '$TMP_CONTENEUR' > /dev/null"
docker cp "houseos-postgres:$TMP_CONTENEUR" "$DESTINATION/.houseos-$HORODATAGE.dump.tmp"
docker exec houseos-postgres rm -f "$TMP_CONTENEUR"
mv "$DESTINATION/.houseos-$HORODATAGE.dump.tmp" "$DESTINATION/houseos-$HORODATAGE.dump"

# 2. Archive du volume de fichiers (manuels et photos des équipements). Le nom du
#    volume est dérivé du conteneur app (dépend du nom du projet compose) ; repli
#    sur le nom historique si l'app ne tourne pas. Même patron tmp+mv que le dump.
VOLUME_FICHIERS="$(docker inspect houseos-app --format \
  '{{ range .Mounts }}{{ if eq .Destination "/app/donnees/fichiers" }}{{ .Name }}{{ end }}{{ end }}' \
  2>/dev/null || true)"
: "${VOLUME_FICHIERS:=house-os_fichiers}"
docker run --rm \
  -v "$VOLUME_FICHIERS":/fichiers:ro \
  -v "$DESTINATION":/backup \
  alpine tar czf "/backup/.fichiers-$HORODATAGE.tar.gz.tmp" -C /fichiers .
mv "$DESTINATION/.fichiers-$HORODATAGE.tar.gz.tmp" "$DESTINATION/fichiers-$HORODATAGE.tar.gz"

# NB : dump et tar ne sont pas simultanés — un upload entre les deux peut donner un
# fichier sans fiche (ou l'inverse). Acceptable pour un foyer ; la restauration
# tolère les orphelins.

# 3. Rétention : purge des backups de plus de RETENTION_JOURS jours
find "$DESTINATION" -name 'houseos-*.dump' -mtime +"$RETENTION_JOURS" -delete
find "$DESTINATION" -name 'fichiers-*.tar.gz' -mtime +"$RETENTION_JOURS" -delete

echo "Backup terminé : $DESTINATION/houseos-$HORODATAGE.dump"
echo "                 $DESTINATION/fichiers-$HORODATAGE.tar.gz"

# Restauration (aide-mémoire) :
#   docker exec -i houseos-postgres pg_restore -U houseos -d houseos --clean --if-exists < houseos-<date>.dump
#   docker run --rm -v house-os_fichiers:/fichiers -v "$PWD":/backup alpine tar xzf /backup/fichiers-<date>.tar.gz -C /fichiers
