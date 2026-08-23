// Illustrations au trait du langage chaleureuse (design/maquettes/Main.dc.html).

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
