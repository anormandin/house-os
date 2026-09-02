import { useState } from 'react'
import { File, FileText, Image as ImageIcon, Mail, type LucideIcon } from 'lucide-react'
import type { Document } from '@/lib/api'
import { cn } from '@/lib/utils'

const LIBELLES_TYPE: Record<string, string> = {
  'application/pdf': 'PDF',
  'image/jpeg': 'JPG',
  'image/png': 'PNG',
  'image/webp': 'WebP',
  'image/heic': 'HEIC',
  'message/rfc822': 'EML',
}

/** Un courriel archivé tel quel (.eml) — voir Documents par courriel. */
export const TYPE_MIME_COURRIEL = 'message/rfc822'

/** Libellé court du type de fichier (PDF, JPG…), replié sur l'extension du nom. */
export function libelleTypeFichier(document: Pick<Document, 'typeMime' | 'nomFichier'>): string {
  const libelle = LIBELLES_TYPE[document.typeMime]
  if (libelle !== undefined) {
    return libelle
  }
  const extension = document.nomFichier.split('.').pop() ?? ''
  return extension.length > 0 && extension.length <= 5 ? extension.toUpperCase() : 'Fichier'
}

/**
 * Aperçu carré d'un document : miniature serveur pour les images (icône si elle
 * échoue — HEIC non décodable, fichier manquant), icône de type sinon.
 */
export default function VignetteDocument({
  document,
  icone,
  classe,
}: {
  document: Document
  /** Icône de repli quand il n'y a pas de miniature (par défaut selon le type). */
  icone?: LucideIcon
  classe?: string
}) {
  const [enErreur, setEnErreur] = useState(false)
  const estImage = document.typeMime.startsWith('image/')

  if (estImage && enErreur === false) {
    return (
      <img
        src={`/api/documents/${document.id}/miniature`}
        alt=""
        loading="lazy"
        onError={() => setEnErreur(true)}
        className={cn('size-11 shrink-0 rounded-lg bg-creux object-cover', classe)}
      />
    )
  }

  const Icone = icone ?? (document.typeMime === 'application/pdf'
    ? FileText
    : document.typeMime === TYPE_MIME_COURRIEL
      ? Mail
      : estImage
        ? ImageIcon
        : File)
  return (
    <div
      className={cn(
        'flex size-11 shrink-0 items-center justify-center rounded-lg bg-creux',
        classe,
      )}
    >
      <Icone className="size-4 text-dore" />
    </div>
  )
}
