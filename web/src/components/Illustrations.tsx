// Illustrations au trait du langage chaleureuse (design/maquettes/Main.dc.html).

import type { IconeCompte } from '@/lib/api'

export function MaisonSoleil({ largeur = 290 }: { largeur?: number }) {
  return (
    <svg
      width={largeur}
      height={(largeur / 300) * 170}
      viewBox="0 0 300 170"
      fill="none"
      aria-hidden="true"
    >
      <circle cx="252" cy="38" r="20" stroke="var(--orange)" strokeWidth="3" />
      <path
        d="M252 8v-6M252 74v-6M282 38h6M216 38h6M273 17l4-4M227 63l4-4M273 59l4 4M227 13l4 4"
        stroke="var(--orange)"
        strokeWidth="3"
        strokeLinecap="round"
      />
      <path d="M40 150h220" stroke="var(--dore)" strokeWidth="3" strokeLinecap="round" />
      <path
        d="M70 150v-52l55-40 55 40v52"
        stroke="var(--encre)"
        strokeWidth="3"
        strokeLinejoin="round"
      />
      <path
        d="M58 104l67-49 67 49"
        stroke="var(--encre)"
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <rect x="110" y="118" width="30" height="32" stroke="var(--orange)" strokeWidth="3" />
      <rect x="85" y="104" width="18" height="16" stroke="var(--dore)" strokeWidth="3" />
      <rect x="147" y="104" width="18" height="16" stroke="var(--dore)" strokeWidth="3" />
      <path
        d="M196 150v-18c0-8 6-14 14-14s14 6 14 14v18"
        stroke="var(--vert)"
        strokeWidth="3"
      />
      <path
        d="M36 150v-14c0-6 5-11 11-11s11 5 11 11v14"
        stroke="var(--vert)"
        strokeWidth="3"
      />
    </svg>
  )
}

export function Camion() {
  return (
    <svg width="34" height="24" viewBox="0 0 40 28" fill="none" aria-hidden="true">
      <rect x="2" y="8" width="22" height="14" rx="2" stroke="var(--orange)" strokeWidth="2.5" />
      <path d="M24 12h8l5 5v5h-13" stroke="var(--orange)" strokeWidth="2.5" strokeLinejoin="round" />
      <circle cx="10" cy="24" r="3" stroke="var(--encre)" strokeWidth="2.5" />
      <circle cx="31" cy="24" r="3" stroke="var(--encre)" strokeWidth="2.5" />
    </svg>
  )
}

export function Sapin() {
  return (
    <svg width="26" height="28" viewBox="0 0 26 30" fill="none" aria-hidden="true">
      <path
        d="M13 2 5 12h4l-6 8h5l-5 7h20l-5-7h5l-6-8h4z"
        stroke="var(--vert)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M13 27v3" stroke="var(--dore)" strokeWidth="2.5" />
    </svg>
  )
}

export function Avion() {
  return (
    <svg width="30" height="26" viewBox="0 0 34 30" fill="none" aria-hidden="true">
      <path
        d="M31 4 3 15l10 4 3 9 5-7"
        stroke="var(--encre)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M31 4 13 19" stroke="var(--orange)" strokeWidth="2.5" strokeLinecap="round" />
    </svg>
  )
}

export function Valise() {
  return (
    <svg width="30" height="26" viewBox="0 0 34 30" fill="none" aria-hidden="true">
      <rect x="3" y="10" width="28" height="18" rx="3" stroke="var(--dore)" strokeWidth="2.5" />
      <path d="M12 10V5h10v5" stroke="var(--encre)" strokeWidth="2.5" strokeLinejoin="round" />
      <path d="M10 10v18M24 10v18" stroke="var(--orange)" strokeWidth="2.5" />
    </svg>
  )
}

export function Gateau() {
  return (
    <svg width="28" height="26" viewBox="0 0 32 30" fill="none" aria-hidden="true">
      <path
        d="M6 28v-9c0-2.5 2-4 4.5-4h11c2.5 0 4.5 1.5 4.5 4v9"
        stroke="var(--rouge)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M3 28h26" stroke="var(--dore)" strokeWidth="2.5" strokeLinecap="round" />
      <path d="M6 21h20" stroke="var(--rouge)" strokeWidth="2.5" />
      <path d="M16 15v-4" stroke="var(--encre)" strokeWidth="2.5" strokeLinecap="round" />
      <circle cx="16" cy="6.5" r="2.5" fill="var(--orange)" />
    </svg>
  )
}

export function Cadeau() {
  return (
    <svg width="28" height="28" viewBox="0 0 32 32" fill="none" aria-hidden="true">
      <rect x="4" y="12" width="24" height="17" rx="2" stroke="var(--vert)" strokeWidth="2.5" />
      <path d="M4 18h24" stroke="var(--vert)" strokeWidth="2.5" />
      <path d="M16 12v17" stroke="var(--orange)" strokeWidth="2.5" />
      <path
        d="M16 11c-2.5-7-11-5.5-8-1 2 3 5.5 1.5 8 1zm0 0c2.5-7 11-5.5 8-1-2 3-5.5 1.5-8 1z"
        stroke="var(--orange)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function Coeur() {
  return (
    <svg width="28" height="26" viewBox="0 0 32 30" fill="none" aria-hidden="true">
      <path
        d="M16 27C7 20.5 3 15 3 10 3 3.5 12 2.8 16 8.5 20 2.8 29 3.5 29 10c0 5-4 10.5-13 17z"
        stroke="var(--rouge)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function Soleil() {
  return (
    <svg width="28" height="28" viewBox="0 0 32 32" fill="none" aria-hidden="true">
      <circle cx="16" cy="16" r="6.5" stroke="var(--orange)" strokeWidth="2.5" />
      <path
        d="M16 5V2M16 30v-3M27 16h3M2 16h3M23.8 8.2l2.1-2.1M6.1 25.9l2.1-2.1M23.8 23.8l2.1 2.1M6.1 6.1l2.1 2.1"
        stroke="var(--orange)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
    </svg>
  )
}

export function Flocon() {
  return (
    <svg width="28" height="28" viewBox="0 0 32 32" fill="none" aria-hidden="true">
      <path
        d="M16 3v26M4.7 9.5l22.6 13M27.3 9.5L4.7 22.5"
        stroke="var(--encre)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      <path
        d="M12 6l4 4 4-4M12 26l4-4 4 4M4 14.5l5 1.5-5 1.5M28 14.5L23 16l5 1.5"
        stroke="var(--encre)"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function Citrouille() {
  return (
    <svg width="28" height="28" viewBox="0 0 32 32" fill="none" aria-hidden="true">
      <ellipse cx="16" cy="19" rx="11" ry="9.5" stroke="var(--orange)" strokeWidth="2.5" />
      <path
        d="M12 10.5c-3 4-3 13 0 17M20 10.5c3 4 3 13 0 17"
        stroke="var(--orange)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      <path
        d="M16 9V6c0-1.7 1.2-3 3-3"
        stroke="var(--vert)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
    </svg>
  )
}

export function Feuille() {
  return (
    <svg width="26" height="28" viewBox="0 0 28 32" fill="none" aria-hidden="true">
      <path
        d="M14 2C6.5 8.5 4.5 16 14 26c9.5-10 7.5-17.5 0-24z"
        stroke="var(--rouge)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path
        d="M14 8v22M14 14l-4-3M14 14l4-3M14 19l-4.5-3.5M14 19l4.5-3.5"
        stroke="var(--dore)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
    </svg>
  )
}

export function Fleur() {
  return (
    <svg width="26" height="28" viewBox="0 0 28 32" fill="none" aria-hidden="true">
      <path
        d="M7 3v6c0 5.5 3 9.5 7 9.5s7-4 7-9.5V3l-4 4-3-4-3 4-4-4z"
        stroke="var(--orange)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M14 18.5V30" stroke="var(--vert)" strokeWidth="2.5" strokeLinecap="round" />
      <path
        d="M14 26c-4.5-.5-6.5-3-7-7 4.5.5 6.5 3 7 7z"
        stroke="var(--vert)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function Tente() {
  return (
    <svg width="32" height="26" viewBox="0 0 36 30" fill="none" aria-hidden="true">
      <path d="M3 27h30" stroke="var(--dore)" strokeWidth="2.5" strokeLinecap="round" />
      <path
        d="M18 4 5 27h9l4-7.5 4 7.5h9L18 4z"
        stroke="var(--vert)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M18 4c-1.5-2-3.5-2.5-5-2" stroke="var(--vert)" strokeWidth="2.5" strokeLinecap="round" />
    </svg>
  )
}

export function Velo() {
  return (
    <svg width="36" height="26" viewBox="0 0 40 28" fill="none" aria-hidden="true">
      <circle cx="9" cy="20" r="6" stroke="var(--encre)" strokeWidth="2.5" />
      <circle cx="31" cy="20" r="6" stroke="var(--encre)" strokeWidth="2.5" />
      <path
        d="M9 20 15 9l5 11 7-11 4 11"
        stroke="var(--orange)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M12.5 9H18M27 9l-3-3" stroke="var(--orange)" strokeWidth="2.5" strokeLinecap="round" />
    </svg>
  )
}

export function Ballon() {
  return (
    <svg width="28" height="28" viewBox="0 0 30 32" fill="none" aria-hidden="true">
      <ellipse cx="10.5" cy="9" rx="6" ry="7" stroke="var(--rouge)" strokeWidth="2.5" />
      <path
        d="M10.5 16c-1.5 5 2 8 1 13"
        stroke="var(--rouge)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      <ellipse cx="22" cy="13" rx="5" ry="6" stroke="var(--orange)" strokeWidth="2.5" />
      <path
        d="M22 19c1 4-1.5 6.5-.5 10"
        stroke="var(--orange)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
    </svg>
  )
}

export function Etoile() {
  return (
    <svg width="28" height="28" viewBox="0 0 32 32" fill="none" aria-hidden="true">
      <path
        d="M16 3l4 8.1 8.9 1.3-6.4 6.3 1.5 8.9-8-4.2-8 4.2 1.5-8.9-6.4-6.3L12 11.1 16 3z"
        stroke="var(--dore)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
    </svg>
  )
}

/** Set d'icônes des comptes à rebours — chaque icône porte sa couleur d'accent. */
export const ICONES_COMPTE: Record<
  IconeCompte,
  { Icone: () => React.JSX.Element; libelle: string; couleur: string }
> = {
  Camion: { Icone: Camion, libelle: 'camion', couleur: 'var(--orange)' },
  Sapin: { Icone: Sapin, libelle: 'sapin', couleur: 'var(--vert)' },
  Avion: { Icone: Avion, libelle: 'avion', couleur: 'var(--encre)' },
  Valise: { Icone: Valise, libelle: 'valise', couleur: 'var(--dore)' },
  Gateau: { Icone: Gateau, libelle: 'gâteau', couleur: 'var(--rouge)' },
  Cadeau: { Icone: Cadeau, libelle: 'cadeau', couleur: 'var(--vert)' },
  Coeur: { Icone: Coeur, libelle: 'cœur', couleur: 'var(--rouge)' },
  Soleil: { Icone: Soleil, libelle: 'soleil', couleur: 'var(--orange)' },
  Flocon: { Icone: Flocon, libelle: 'flocon', couleur: 'var(--encre)' },
  Citrouille: { Icone: Citrouille, libelle: 'citrouille', couleur: 'var(--orange)' },
  Feuille: { Icone: Feuille, libelle: 'feuille', couleur: 'var(--rouge)' },
  Fleur: { Icone: Fleur, libelle: 'fleur', couleur: 'var(--orange)' },
  Tente: { Icone: Tente, libelle: 'tente', couleur: 'var(--vert)' },
  Velo: { Icone: Velo, libelle: 'vélo', couleur: 'var(--encre)' },
  Ballon: { Icone: Ballon, libelle: 'ballon', couleur: 'var(--rouge)' },
  Etoile: { Icone: Etoile, libelle: 'étoile', couleur: 'var(--dore)' },
}
