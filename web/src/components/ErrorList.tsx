import { InlineNotification } from '@carbon/react'

interface ErrorListProps {
  title: string
  messages: string[]
}

export function ErrorList({ title, messages }: ErrorListProps) {
  return (
    <InlineNotification kind="error" title={title} hideCloseButton lowContrast>
      <ul>
        {messages.map((message) => (
          <li key={message}>{message}</li>
        ))}
      </ul>
    </InlineNotification>
  )
}
