import type { ReactNode } from 'react'
import { Text } from './Text'
import styles from './Card.module.css'

interface CardProps {
  children: ReactNode
  variant?: 'default' | 'primary' | 'danger'
  title?: string
  className?: string
}

export function Card({ children, variant = 'default', title, className = '' }: CardProps) {
  const classNames = [styles.card, styles[variant], className].join(' ')

  return (
    <section className={classNames}>
      {title && (
        <header className={styles.header}>
          <Text variant="subtitle" as="h3">
            {title}
          </Text>
        </header>
      )}
      <div className={styles.body}>{children}</div>
    </section>
  )
}
