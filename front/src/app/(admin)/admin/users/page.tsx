'use client';

import { useState, useEffect, useMemo } from 'react';
import { Search, Shield, ShieldOff, Eye, UserX, Loader2 } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { usersService, ordersService } from '@/lib/api-services';
import type { UserDto, OrderDto } from '@/lib/api-types';
import { UserRole, UserStatus } from '@/lib/enums';
import { enumLabel } from '@/lib/enums';
import { toast } from 'sonner';

const STATUS_COLORS: Record<number, string> = {
  [UserStatus.Active]: 'text-success border-success',
  [UserStatus.Inactive]: 'text-muted-foreground',
};

export default function AdminUsersPage() {
  const { locale } = useI18n();
  const [usersList, setUsersList] = useState<UserDto[]>([]);
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [roleFilter, setRoleFilter] = useState('all');
  const [detailUser, setDetailUser] = useState<UserDto | null>(null);
  const [confirmAnon, setConfirmAnon] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [usersRes, ordersRes] = await Promise.all([
          usersService.getAll(1, 100),
          ordersService.getAll(1, 200),
        ]);
        setUsersList(usersRes.data);
        setOrders(ordersRes.data);
      } catch (err) {
        console.error('Failed to load users', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = useMemo(() => {
    let list = [...usersList].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(u => u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
    }
    if (statusFilter !== 'all') list = list.filter(u => u.status === Number(statusFilter));
    if (roleFilter !== 'all') list = list.filter(u => u.role === Number(roleFilter));
    return list;
  }, [usersList, search, statusFilter, roleFilter]);

  const getUserOrders = (userId: string) => orders.filter(o => o.userId === userId);

  const handleAnonymize = async (userId: string) => {
    try {
      const updated = await usersService.anonymize(userId);
      setUsersList(prev => prev.map(u => u.id === userId ? updated : u));
      setConfirmAnon(null);
      setDetailUser(null);
      toast.success(locale === 'fr' ? 'Utilisateur anonymisé (RGPD)' : 'User anonymized (GDPR)');
    } catch (err) {
      console.error('Failed to anonymize user', err);
      toast.error(locale === 'fr' ? 'Erreur lors de l\'anonymisation' : 'Failed to anonymize user');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder={locale === 'fr' ? 'Rechercher un utilisateur...' : 'Search users...'} className="ps-9" />
        </div>
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-[130px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous statuts' : 'All statuses'}</SelectItem>
            <SelectItem value={String(UserStatus.Active)}>{enumLabel('UserStatus', UserStatus.Active, locale)}</SelectItem>
            <SelectItem value={String(UserStatus.Inactive)}>{enumLabel('UserStatus', UserStatus.Inactive, locale)}</SelectItem>
          </SelectContent>
        </Select>
        <Select value={roleFilter} onValueChange={setRoleFilter}>
          <SelectTrigger className="w-[120px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{locale === 'fr' ? 'Tous rôles' : 'All roles'}</SelectItem>
            <SelectItem value={String(UserRole.Customer)}>{enumLabel('UserRole', UserRole.Customer, locale)}</SelectItem>
            <SelectItem value={String(UserRole.Admin)}>{enumLabel('UserRole', UserRole.Admin, locale)}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-gray-50">
                  <th className="p-3 text-start">{locale === 'fr' ? 'Utilisateur' : 'User'}</th>
                  <th className="p-3 text-start">Email</th>
                  <th className="p-3 text-center">{locale === 'fr' ? 'Rôle' : 'Role'}</th>
                  <th className="p-3 text-center">Status</th>
                  <th className="p-3 text-center">{locale === 'fr' ? 'Commandes' : 'Orders'}</th>
                  <th className="p-3 text-start">{locale === 'fr' ? 'Dernière connexion' : 'Last login'}</th>
                  <th className="p-3 text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(user => (
                  <tr key={user.id} className={`border-b hover:bg-gray-50/50 ${user.anonymized ? 'opacity-50' : ''}`}>
                    <td className="p-3">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-brand-dark">{user.name}</span>
                        {user.anonymized && <Badge variant="secondary" className="text-[10px]">RGPD</Badge>}
                      </div>
                    </td>
                    <td className="p-3 text-muted-foreground">{user.email}</td>
                    <td className="p-3 text-center">
                      {user.role === UserRole.Admin ? (
                        <Badge className="bg-brand-primary/10 text-brand-primary"><Shield className="w-3 h-3 me-1" />Admin</Badge>
                      ) : (
                        <Badge variant="outline">{enumLabel('UserRole', user.role, locale)}</Badge>
                      )}
                    </td>
                    <td className="p-3 text-center">
                      <Badge variant="outline" className={STATUS_COLORS[user.status] || ''}>
                        {enumLabel('UserStatus', user.status, locale)}
                      </Badge>
                    </td>
                    <td className="p-3 text-center">{getUserOrders(user.id).length}</td>
                    <td className="p-3 text-muted-foreground text-xs">{user.lastLogin ? new Date(user.lastLogin).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US') : '-'}</td>
                    <td className="p-3 text-end">
                      <div className="flex items-center justify-end gap-1">
                        <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => setDetailUser(user)}>
                          <Eye className="w-3.5 h-3.5" />
                        </Button>
                        {!user.anonymized && user.role !== UserRole.Admin && (
                          <Button size="icon" variant="ghost" className="h-7 w-7 text-warning" onClick={() => setConfirmAnon(user.id)}>
                            <UserX className="w-3.5 h-3.5" />
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {/* User Detail */}
      <Dialog open={!!detailUser} onOpenChange={() => setDetailUser(null)}>
        <DialogContent className="max-w-md">
          {detailUser && (
            <>
              <DialogHeader>
                <DialogTitle>{detailUser.name}</DialogTitle>
              </DialogHeader>
              <div className="space-y-3 text-sm">
                <div className="grid grid-cols-2 gap-3">
                  <div><p className="text-muted-foreground">Email</p><p>{detailUser.email}</p></div>
                  <div><p className="text-muted-foreground">{locale === 'fr' ? 'Rôle' : 'Role'}</p><p>{enumLabel('UserRole', detailUser.role, locale)}</p></div>
                  <div><p className="text-muted-foreground">Status</p><p>{enumLabel('UserStatus', detailUser.status, locale)}</p></div>
                  <div><p className="text-muted-foreground">{locale === 'fr' ? 'Email confirmé' : 'Email confirmed'}</p><p>{detailUser.emailConfirmed ? 'Yes' : 'No'}</p></div>
                  <div><p className="text-muted-foreground">{locale === 'fr' ? 'Créé le' : 'Created'}</p><p>{new Date(detailUser.createdAt).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</p></div>
                  <div><p className="text-muted-foreground">{locale === 'fr' ? 'Dernière connexion' : 'Last login'}</p><p>{detailUser.lastLogin ? new Date(detailUser.lastLogin).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US') : '-'}</p></div>
                </div>
                <Separator />
                <div>
                  <p className="font-medium mb-1">{locale === 'fr' ? 'Adresses' : 'Addresses'} ({detailUser.addresses.length})</p>
                  {detailUser.addresses.map(a => (
                    <div key={a.id} className="text-xs text-muted-foreground mb-1">
                      {a.label}: {a.street}, {a.postalCode} {a.city}
                    </div>
                  ))}
                  {detailUser.addresses.length === 0 && <p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Aucune adresse' : 'No addresses'}</p>}
                </div>
                <div>
                  <p className="font-medium mb-1">{locale === 'fr' ? 'Commandes' : 'Orders'} ({getUserOrders(detailUser.id).length})</p>
                  {getUserOrders(detailUser.id).map(o => (
                    <div key={o.id} className="text-xs text-muted-foreground">#{o.id.split('-').pop()} - {enumLabel('OrderStatus', o.status, locale)} - {new Date(o.date).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</div>
                  ))}
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Anonymize confirm */}
      <Dialog open={!!confirmAnon} onOpenChange={() => setConfirmAnon(null)}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle className="text-error">{locale === 'fr' ? 'Anonymiser cet utilisateur ?' : 'Anonymize this user?'}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {locale === 'fr'
              ? 'Cette action est irréversible. Les données personnelles seront supprimées conformément au RGPD. Les commandes seront conservées de manière anonyme.'
              : 'This action is irreversible. Personal data will be removed in compliance with GDPR. Orders will be kept anonymously.'}
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmAnon(null)}>{locale === 'fr' ? 'Annuler' : 'Cancel'}</Button>
            <Button variant="destructive" onClick={() => confirmAnon && handleAnonymize(confirmAnon)}>
              {locale === 'fr' ? 'Anonymiser' : 'Anonymize'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
