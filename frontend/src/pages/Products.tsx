import { FormEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import type { Product } from '../lib/types';

export function Products() {
  const [products, setProducts] = useState<Product[]>([]);
  const [name, setName] = useState(''); const [slug, setSlug] = useState(''); const [error, setError] = useState('');
  const load = () => api.products().then(setProducts).catch(e => setError(e.message));
  useEffect(() => { void load(); }, []);
  async function submit(e: FormEvent) { e.preventDefault(); try { await api.createProduct({ name, slug, description: null }); setName(''); setSlug(''); void load(); } catch (e) { setError((e as Error).message); } }
  return <section><div className="page-heading"><div><p className="eyebrow">Catalog</p><h2>Products</h2></div></div>{error && <p className="error">{error}</p>}<div className="grid"><div className="card"><h3>Registered products</h3>{products.length === 0 ? <p className="muted">No products registered yet.</p> : <ul className="rows">{products.map(p => <li key={p.id}><Link to={`/products/${p.id}`}>{p.name}</Link><span>{p.status} · {p.slug}</span></li>)}</ul>}</div><form className="card form" onSubmit={submit}><h3>Add product</h3><input required placeholder="Name" value={name} onChange={e => setName(e.target.value)}/><input required placeholder="slug" value={slug} onChange={e => setSlug(e.target.value)}/><button type="submit">Create product</button></form></div></section>;
}
