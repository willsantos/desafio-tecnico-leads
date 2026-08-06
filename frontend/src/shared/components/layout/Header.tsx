import { NavLink } from 'react-router-dom'
import styles from './Header.module.css'

const NAV_ITEMS = [
  { to: '/', label: 'Nova proposta' },
  { to: '/leads', label: 'Propostas' },
]

export function Header() {
  return (
    <nav className={styles.header} aria-label="Navegação principal">
      <ul className={styles.list}>
        {NAV_ITEMS.map((item) => (
          <li key={item.to}>
            <NavLink
              to={item.to}
              className={({ isActive }) => [styles.link, isActive ? styles.active : ''].join(' ')}
              end
            >
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
