import React, { useState } from 'react';
import api from '../services/api';
import StatusBadge from './StatusBadge';

const EXAMPLES = [
  'Quel est mon bureau actuel ?',
  "Quel matériel m'est confié ?",
  'Combien ai-je eu d’affectations au total ?',
];

/**
 * Floating chat bubble for the Agent's self-service AI assistant — sits fixed in the
 * corner of the viewport so it's reachable from any page, instead of living as its own
 * sidebar destination.
 */
const AssistantChatBubble = () => {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const sendMessage = async (text) => {
    const message = text.trim();
    if (!message || loading) return;

    setMessages((prev) => [...prev, { role: 'user', text: message }]);
    setInput('');
    setLoading(true);
    setError('');

    try {
      const response = await api.post('/agents/me/chat', { message });
      setMessages((prev) => [...prev, { role: 'assistant', text: response.data.answer, usedAi: !!response.data.usedAi }]);
    } catch (err) {
      console.error('Agent chat error:', err);
      setError(err.response?.data?.detail || "L'assistant n'a pas pu répondre.");
    } finally { setLoading(false); }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    sendMessage(input);
  };

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col items-end gap-3">
      {open && (
        <div className="w-[340px] max-h-[70vh] flex flex-col bg-surface-bg border border-border-subtle shadow-2xl">
          <div className="flex items-center justify-between bg-primary px-4 py-3 flex-shrink-0">
            <div className="text-[13px] font-bold text-white" style={{ fontFamily: 'var(--font-display)' }}>
              Assistant IA
            </div>
            <button
              onClick={() => setOpen(false)}
              className="text-white/70 hover:text-white text-[18px] leading-none"
              aria-label="Fermer"
            >
              ×
            </button>
          </div>

          <div className="flex-1 min-h-[160px] overflow-y-auto p-3 flex flex-col gap-2.5">
            {messages.length === 0 && (
              <div className="text-[12px] text-text-secondary italic px-1">
                Posez une question sur votre bureau, votre matériel ou votre historique.
              </div>
            )}
            {messages.map((m, i) => (
              <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[85%] px-3 py-2 text-[12.5px] ${
                    m.role === 'user'
                      ? 'bg-primary text-white'
                      : 'bg-neutral-bg text-text-primary border-l-[3px] border-accent'
                  }`}
                >
                  <div>{m.text}</div>
                  {m.role === 'assistant' && (
                    <div className="mt-1">
                      <StatusBadge tone={m.usedAi ? 'success' : 'warning'}>{m.usedAi ? 'IA activée' : 'Repli mot-clé'}</StatusBadge>
                    </div>
                  )}
                </div>
              </div>
            ))}
            {loading && <div className="text-[11.5px] text-text-secondary italic px-1">L'assistant réfléchit...</div>}
            {error && <div className="text-[11.5px] text-danger px-1">{error}</div>}
          </div>

          {messages.length === 0 && (
            <div className="px-3 pb-2 flex flex-wrap gap-1.5 flex-shrink-0">
              {EXAMPLES.map((ex) => (
                <button
                  key={ex}
                  type="button"
                  onClick={() => sendMessage(ex)}
                  disabled={loading}
                  className="text-[10.5px] text-text-secondary hover:text-primary border border-border-subtle px-2 py-1 transition-colors disabled:opacity-50"
                >
                  {ex}
                </button>
              ))}
            </div>
          )}

          <form onSubmit={handleSubmit} className="flex gap-2 p-3 border-t border-border-subtle flex-shrink-0">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Posez votre question..."
              className="form-field flex-1 text-[12.5px]"
            />
            <button
              type="submit"
              disabled={loading || !input.trim()}
              className="bg-primary px-3 py-2 text-[11px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50 whitespace-nowrap"
              style={{ fontFamily: 'var(--font-mono)' }}
            >
              Envoyer
            </button>
          </form>
        </div>
      )}

      <button
        onClick={() => setOpen((v) => !v)}
        className="w-14 h-14 rounded-full bg-primary hover:bg-primary-dark text-white shadow-xl flex items-center justify-center transition-colors"
        style={{ fontFamily: 'var(--font-display)' }}
        aria-label={open ? "Fermer l'assistant IA" : "Ouvrir l'assistant IA"}
        title="Assistant IA"
      >
        {open ? (
          <span className="text-[22px] leading-none">×</span>
        ) : (
          <span className="text-[11px] font-bold tracking-wide">IA</span>
        )}
      </button>
    </div>
  );
};

export default AssistantChatBubble;
